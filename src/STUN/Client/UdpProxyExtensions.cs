using STUN.Messages;
using STUN.Proxy;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace STUN.Client;

internal static class UdpProxyExtensions
{
	public static async ValueTask<StunResponse?> RequestAsync(this IUdpProxy proxy, StunMessage5389 request, IPEndPoint remote, TimeSpan timeout, CancellationToken cancellationToken)
	{
		try
		{
			using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(0x10000);
			Memory<byte> buffer = memoryOwner.Memory;
			int length = request.WriteTo(buffer.Span);

			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			// .NET 第一次接收时才开启包信息，之前到达的响应没有目的地址，所以在发送前开启
			EnablePacketInformation(proxy.Client);

			// DTLS 代理在发送时握手，发送也要计时
			cts.CancelAfter(timeout);
			await proxy.SendToAsync(buffer.Slice(0, length), SocketFlags.None, remote, cts.Token);

			// 再次调用 CancelAfter 会重新计时；接收只计时这一次，跳过的数据报不会延长等待
			cts.CancelAfter(timeout);
			return await ReceiveResponseAsync(proxy, buffer, request, remote, cts.Token);
		}
		catch (OperationCanceledException ex)
		{
			Debug.WriteLine(ex);
		}

		return default;
	}

	private static void EnablePacketInformation(Socket socket)
	{
		SocketOptionLevel level = socket.AddressFamily is AddressFamily.InterNetworkV6 ? SocketOptionLevel.IPv6 : SocketOptionLevel.IP;
		socket.SetSocketOption(level, SocketOptionName.PacketInformation, true);
	}

	private static async ValueTask<StunResponse> ReceiveResponseAsync(IUdpProxy proxy, Memory<byte> buffer, StunMessage5389 request, IPEndPoint remote, CancellationToken cancellationToken)
	{
		while (true)
		{
			cancellationToken.ThrowIfCancellationRequested();

			SocketReceiveMessageFromResult result;

			try
			{
				// remote 仅用于提供 AddressFamily，UDP 下不过滤来源
				result = await proxy.ReceiveMessageFromAsync(buffer, SocketFlags.None, remote, cancellationToken);
			}
			catch (SocketException ex) when (ex.SocketErrorCode is SocketError.ConnectionReset)
			{
				// Windows 上之前某个数据报引发的 ICMP 端口不可达，与跳过的数据报一样处理
				Debug.WriteLine(ex);
				continue;
			}

			StunMessage5389 response = new();

			if (response.TryParse(buffer.Slice(0, result.ReceivedBytes)) && response.IsSameTransaction(request))
			{
				return new StunResponse(response, (IPEndPoint)result.RemoteEndPoint, new IPEndPoint(result.PacketInformation.Address, GetLocalEndPoint(proxy).Port));
			}
		}
	}

	private static IPEndPoint GetLocalEndPoint(IUdpProxy proxy)
	{
		return proxy.Client.LocalEndPoint as IPEndPoint
				?? throw new InvalidOperationException(@"UDP client local endpoint is unavailable.");
	}
}
