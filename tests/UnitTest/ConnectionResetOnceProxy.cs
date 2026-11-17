using STUN.Proxy;
using System.Net;
using System.Net.Sockets;

namespace UnitTest;

/// <summary>
/// Simulates a Windows socket that keeps SIO_UDP_CONNRESET enabled, such as the UDP socket the SOCKS5 library creates,
/// where ICMP port unreachable caused by an earlier datagram fails the next receive.
/// </summary>
internal sealed class ConnectionResetOnceProxy(IUdpProxy inner) : IUdpProxy
{
	private bool _resetPending = true;

	public Socket Client => inner.Client;

	public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
	{
		return inner.ConnectAsync(cancellationToken);
	}

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		return inner.CloseAsync(cancellationToken);
	}

	public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
	{
		if (_resetPending)
		{
			_resetPending = false;
			return ValueTask.FromException<SocketReceiveMessageFromResult>(new SocketException((int)SocketError.ConnectionReset));
		}

		return inner.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, cancellationToken);
	}

	public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
	{
		return inner.SendToAsync(buffer, socketFlags, remoteEP, cancellationToken);
	}

	public void Dispose()
	{
		inner.Dispose();
	}

	public ValueTask DisposeAsync()
	{
		return inner.DisposeAsync();
	}
}
