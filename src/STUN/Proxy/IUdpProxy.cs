using System.Net;
using System.Net.Sockets;

namespace STUN.Proxy;

/// <summary>
/// Defines a UDP proxy that can send and receive datagrams, optionally through an intermediary.
/// </summary>
public interface IUdpProxy : IDisposable, IAsyncDisposable
{
	/// <summary>
	/// Gets the underlying socket used for UDP communication.
	/// </summary>
	Socket Client { get; }

	/// <summary>
	/// Establishes the proxy connection.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous connect operation.</returns>
	ValueTask ConnectAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Closes the proxy connection and releases associated resources.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous close operation.</returns>
	ValueTask CloseAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Receives a UDP datagram and returns the sender information.
	/// </summary>
	/// <param name="buffer">The buffer to store the received data.</param>
	/// <param name="socketFlags">The socket flags for the receive operation.</param>
	/// <param name="remoteEndPoint">The remote endpoint to receive from.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The result containing received bytes, remote endpoint, and packet information.</returns>
	ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sends a UDP datagram to the specified remote endpoint.
	/// </summary>
	/// <param name="buffer">The data to send.</param>
	/// <param name="socketFlags">The socket flags for the send operation.</param>
	/// <param name="remoteEP">The destination endpoint.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The number of bytes sent.</returns>
	ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default);
}
