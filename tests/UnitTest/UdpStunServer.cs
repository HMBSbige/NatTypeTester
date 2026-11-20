using STUN.Enums;
using STUN.Messages;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using static STUN.Utils.AttributeExtensions;

namespace UnitTest;

/// <summary>
/// Datagrams that the server sends to the client before its response.
/// </summary>
public enum Interference
{
	None,

	/// <summary>
	/// A valid response in another transaction, such as the response to an earlier request that arrives after that request timed out.
	/// </summary>
	StrayResponse,

	NotStun
}

internal sealed class UdpStunServer : IAsyncDisposable
{
	/// <summary>
	/// The delay between the interference and the response, so that the interference arrives first.
	/// </summary>
	private static readonly TimeSpan InterferenceLead = TimeSpan.FromMilliseconds(50);

	/// <summary>
	/// The interval between repeated interference when not responding, far shorter than the ReceiveTimeout the tests use.
	/// </summary>
	private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(100);

	/// <summary>
	/// The mapped address in the response of another transaction, which a client gets if it wrongly accepts that response.
	/// </summary>
	private static readonly IPEndPoint StrayMappedAddress = IPEndPoint.Parse(@"198.51.100.9:9");

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);

	private readonly Socket _socket;
	private readonly Socket _responseSocket;
	private readonly CancellationTokenSource _cts = new();
	private readonly Task _serveTask;

	public IPEndPoint EndPoint { get; }

	private UdpStunServer(IPEndPoint? mappedAddress, Interference interference)
	{
		_socket = UdpSockets.Bind(IPAddress.Loopback, 0);

		try
		{
			_responseSocket = UdpSockets.Bind(IPAddress.Loopback, 0);
		}
		catch
		{
			_socket.Dispose();
			throw;
		}

		EndPoint = UdpSockets.GetLocalEndPoint(_socket);
		_serveTask = Task.Run(() => ServeAsync(mappedAddress, interference, _cts.Token));
	}

	/// <summary>
	/// Answers each request with <paramref name="interference"/> first, then with a response carrying <paramref name="mappedAddress"/> from another port,
	/// so a client must both skip the interference and accept a response from any source.
	/// </summary>
	public static UdpStunServer Responding(IPEndPoint mappedAddress, Interference interference)
	{
		return new UdpStunServer(mappedAddress, interference);
	}

	/// <summary>
	/// Never responds. After receiving a request, sends <paramref name="interference"/>, unless it is <see cref="Interference.None"/>,
	/// every <see cref="RepeatInterval"/> until the server is disposed.
	/// </summary>
	public static UdpStunServer Unresponsive(Interference interference)
	{
		return new UdpStunServer(null, interference);
	}

	private async Task ServeAsync(IPEndPoint? mappedAddress, Interference interference, CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[0x10000];

		while (!cancellationToken.IsCancellationRequested)
		{
			SocketReceiveFromResult received = await _socket.ReceiveFromAsync(buffer, SocketFlags.None, Any, cancellationToken);
			StunMessage5389 request = new();

			if (!request.TryParse(buffer.AsMemory(0, received.ReceivedBytes)))
			{
				continue;
			}

			if (mappedAddress is null)
			{
				if (interference is Interference.None)
				{
					continue;
				}

				while (true)
				{
					await _socket.SendToAsync(CreateInterference(request, interference), SocketFlags.None, received.RemoteEndPoint, cancellationToken);
					await Task.Delay(RepeatInterval, cancellationToken);
				}
			}

			if (interference is not Interference.None)
			{
				await _socket.SendToAsync(CreateInterference(request, interference), SocketFlags.None, received.RemoteEndPoint, cancellationToken);
				await Task.Delay(InterferenceLead, cancellationToken);
			}

			StunMessage5389 response = new()
			{
				StunMessageType = StunMessageType.BindingResponse,
				MagicCookie = request.MagicCookie,
				Attributes = [BuildMapping(IpFamily.IPv4, mappedAddress.Address, (ushort)mappedAddress.Port)]
			};
			request.TransactionId.CopyTo(response.TransactionId);

			await _responseSocket.SendToAsync(Serialize(response), SocketFlags.None, received.RemoteEndPoint, cancellationToken);
		}
	}

	private static ReadOnlyMemory<byte> CreateInterference(StunMessage5389 request, Interference interference)
	{
		return interference switch
		{
			// A new message comes with a random transaction ID
			Interference.StrayResponse => Serialize(new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingResponse,
				MagicCookie = request.MagicCookie,
				Attributes = [BuildMapping(IpFamily.IPv4, StrayMappedAddress.Address, (ushort)StrayMappedAddress.Port)]
			}),
			Interference.NotStun => RawStunMessage.NotStun,
			_ => throw new UnreachableException()
		};
	}

	private static byte[] Serialize(StunMessage5389 message)
	{
		byte[] buffer = new byte[message.Length];
		message.WriteTo(buffer);
		return buffer;
	}

	public async ValueTask DisposeAsync()
	{
		await _cts.CancelAsync();

		try
		{
			await _serveTask;
		}
		catch (OperationCanceledException)
		{
			// Stopped normally
		}
		catch (SocketException)
		{
			// On Windows, once the client socket is closed, ICMP port unreachable for datagrams sent to it
			// makes the server's sends and receives throw ConnectionReset
		}
		finally
		{
			_socket.Dispose();
			_responseSocket.Dispose();
			_cts.Dispose();
		}
	}
}
