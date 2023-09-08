using Microsoft;
using STUN.Enums;
using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using STUN.Utils;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace STUN.Client;

/// <summary>
/// https://tools.ietf.org/html/rfc3489#section-10.1
/// </summary>
public class StunClient3489 : IUdpStunClient
{
	public virtual IPEndPoint LocalEndPoint => (IPEndPoint)_proxy.Client.LocalEndPoint!;

	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3);

	private readonly IPEndPoint _remoteEndPoint;

	private readonly IUdpProxy _proxy;

	public ClassicStunResult State { get; private set; } = new();

	public StunClient3489(IPEndPoint server, IPEndPoint local, IUdpProxy? proxy = null)
	{
		Requires.NotNull(server, nameof(server));
		Requires.NotNull(local, nameof(local));

		_proxy = proxy ?? new NoneUdpProxy(local);

		_remoteEndPoint = server;

		State.LocalEndPoint = local;
	}

	public async ValueTask ConnectProxyAsync(CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(ReceiveTimeout);

		await _proxy.ConnectAsync(cts.Token);
	}

	public async ValueTask CloseProxyAsync(CancellationToken cancellationToken = default)
	{
		await _proxy.CloseAsync(cancellationToken);
	}

	public async ValueTask QueryAsync(CancellationToken cancellationToken = default)
	{
		State = new ClassicStunResult();

		// test I
		StunResponse? response1 = await Test1Async(cancellationToken);
		if (response1 is null)
		{
			State.NatType = NatType.UdpBlocked;
			return;
		}

		State.LocalEndPoint = response1.Local;

		IPEndPoint? mappedAddress1 = response1.Message.GetMappedAddressAttribute();
		IPEndPoint? changedAddress = response1.Message.GetChangedAddressAttribute();

		State.PublicEndPoint = mappedAddress1; // 显示 test I 得到的映射地址

		// 某些单 IP 服务器的迷惑操作
		if (mappedAddress1 is null || changedAddress is null
								   || Equals(changedAddress.Address, response1.Remote.Address)
								   || changedAddress.Port == response1.Remote.Port)
		{
			State.NatType = NatType.UnsupportedServer;
			return;
		}

		// test II
		StunResponse? response2 = await Test2Async(changedAddress, cancellationToken);
		IPEndPoint? mappedAddress2 = response2?.Message.GetMappedAddressAttribute();

		if (response2 is not null)
		{
			// 有些单 IP 服务器并不能测 NAT 类型
			if (Equals(response1.Remote.Address, response2.Remote.Address) || response1.Remote.Port == response2.Remote.Port)
			{
				State.NatType = NatType.UnsupportedServer;
				State.PublicEndPoint = mappedAddress2;
				return;
			}
		}

		// is Public IP == link's IP?
		if (Equals(mappedAddress1, response1.Local))
		{
			// No NAT
			if (response2 is null)
			{
				State.NatType = NatType.SymmetricUdpFirewall;
				State.PublicEndPoint = mappedAddress1;
			}
			else
			{
				State.NatType = NatType.OpenInternet;
				State.PublicEndPoint = mappedAddress2;
			}
			return;
		}

		// NAT
		if (response2 is not null)
		{
			State.NatType = NatType.FullCone;
			State.PublicEndPoint = mappedAddress2;
			return;
		}

		// Test I(#2)
		StunResponse? response12 = await Test1_2Async(changedAddress, cancellationToken);
		IPEndPoint? mappedAddress12 = response12?.Message.GetMappedAddressAttribute();

		if (mappedAddress12 is null)
		{
			State.NatType = NatType.Unknown;
			return;
		}

		if (!Equals(mappedAddress12, mappedAddress1))
		{
			State.NatType = NatType.Symmetric;
			State.PublicEndPoint = mappedAddress12;
			return;
		}

		// Test III
		StunResponse? response3 = await Test3Async(cancellationToken);
		if (response3 is not null)
		{
			IPEndPoint? mappedAddress3 = response3.Message.GetMappedAddressAttribute();
			if (mappedAddress3 is not null
				&& Equals(response3.Remote.Address, response1.Remote.Address)
				&& response3.Remote.Port != response1.Remote.Port)
			{
				State.NatType = NatType.RestrictedCone;
				State.PublicEndPoint = mappedAddress3;
				return;
			}
		}

		State.NatType = NatType.PortRestrictedCone;
		State.PublicEndPoint = mappedAddress12;
	}

	private async ValueTask<StunResponse?> RequestAsync(StunMessage5389 sendMessage, IPEndPoint remote, IPEndPoint receive, CancellationToken cancellationToken)
	{
		try
		{
			using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(0x10000);
			Memory<byte> buffer = memoryOwner.Memory;
			int length = sendMessage.WriteTo(buffer.Span);

			await _proxy.SendToAsync(buffer[..length], SocketFlags.None, remote, cancellationToken);

			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(ReceiveTimeout);
			SocketReceiveMessageFromResult r = await _proxy.ReceiveMessageFromAsync(buffer, SocketFlags.None, receive, cts.Token);

			StunMessage5389 message = new();
			if (message.TryParse(buffer[..r.ReceivedBytes]) && message.IsSameTransaction(sendMessage))
			{
				return new StunResponse(message, (IPEndPoint)r.RemoteEndPoint, new IPEndPoint(r.PacketInformation.Address, ((IPEndPoint)_proxy.Client.LocalEndPoint!).Port));
			}
		}
		catch (OperationCanceledException ex)
		{
			Debug.WriteLine(ex);
		}
		return default;
	}

	public virtual async ValueTask<StunResponse?> Test1Async(CancellationToken cancellationToken)
	{
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			MagicCookie = 0
		};
		return await RequestAsync(message, _remoteEndPoint, _remoteEndPoint, cancellationToken);
	}

	public virtual async ValueTask<StunResponse?> Test2Async(IPEndPoint other, CancellationToken cancellationToken)
	{
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			MagicCookie = 0,
			Attributes = new[] { AttributeExtensions.BuildChangeRequest(true, true) }
		};
		return await RequestAsync(message, _remoteEndPoint, other, cancellationToken);
	}

	public virtual async ValueTask<StunResponse?> Test1_2Async(IPEndPoint other, CancellationToken cancellationToken)
	{
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			MagicCookie = 0
		};
		return await RequestAsync(message, other, other, cancellationToken);
	}

	public virtual async ValueTask<StunResponse?> Test3Async(CancellationToken cancellationToken)
	{
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			MagicCookie = 0,
			Attributes = new[] { AttributeExtensions.BuildChangeRequest(false, true) }
		};
		return await RequestAsync(message, _remoteEndPoint, _remoteEndPoint, cancellationToken);
	}

	public void Dispose()
	{
		_proxy.Dispose();

		GC.SuppressFinalize(this);
	}
}
