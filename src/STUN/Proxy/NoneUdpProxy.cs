using System.Net;
using System.Net.Sockets;

namespace STUN.Proxy;

/// <summary>
/// A UDP proxy that communicates directly without any intermediary.
/// </summary>
public class NoneUdpProxy : IUdpProxy
{
	/// <inheritdoc />
	public Socket Client { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="NoneUdpProxy"/> class bound to the specified local endpoint.
	/// </summary>
	/// <param name="localEndPoint">The local endpoint to bind the UDP socket to.</param>
	public NoneUdpProxy(IPEndPoint localEndPoint)
	{
		ArgumentNullException.ThrowIfNull(localEndPoint);

		Client = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
		Client.Bind(localEndPoint);
	}

	/// <inheritdoc />
	public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
	{
		return default;
	}

	/// <inheritdoc />
	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		return default;
	}

	/// <inheritdoc />
	public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
	{
		return Client.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
	{
		return Client.SendToAsync(buffer, socketFlags, remoteEP, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		Dispose();
		GC.SuppressFinalize(this);
		return default;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Client.Dispose();
		GC.SuppressFinalize(this);
	}
}
