using STUN.Proxy;
using System.Net;
using System.Net.Sockets;

namespace UnitTest;

/// <summary>
/// Starts each receive only once a datagram is queued on the socket, as when a response arrives before the client gets to receive it.
/// </summary>
internal sealed class LateReceiveProxy(IUdpProxy inner) : IUdpProxy
{
	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(10);

	public Socket Client => inner.Client;

	public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
	{
		return inner.ConnectAsync(cancellationToken);
	}

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		return inner.CloseAsync(cancellationToken);
	}

	public async ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
	{
		while (inner.Client.Available is 0)
		{
			await Task.Delay(PollInterval, cancellationToken);
		}

		return await inner.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, cancellationToken);
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
