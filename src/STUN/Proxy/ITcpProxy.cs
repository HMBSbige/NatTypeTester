using System.IO.Pipelines;
using System.Net;

namespace STUN.Proxy;

/// <summary>
/// Defines a TCP proxy that can establish connections and provide duplex communication pipes.
/// </summary>
public interface ITcpProxy : IDisposable
{
	/// <summary>
	/// Gets the local endpoint currently used by the proxy connection.
	/// </summary>
	IPEndPoint? CurrentLocalEndPoint { get; }

	/// <summary>
	/// Connects to the specified destination endpoint using the given local endpoint.
	/// </summary>
	/// <param name="local">The local endpoint to bind to.</param>
	/// <param name="dst">The destination endpoint to connect to.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A duplex pipe for bidirectional communication.</returns>
	ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default);

	/// <summary>
	/// Closes the current proxy connection.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task representing the asynchronous close operation.</returns>
	ValueTask CloseAsync(CancellationToken cancellationToken = default);
}
