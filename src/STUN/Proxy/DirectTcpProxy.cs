using Pipelines.Extensions;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace STUN.Proxy;

/// <summary>
/// A TCP proxy that connects directly to the destination without any intermediary.
/// </summary>
public class DirectTcpProxy : ITcpProxy
{
	/// <inheritdoc />
	public IPEndPoint? CurrentLocalEndPoint
	{
		get
		{
			ObjectDisposedException.ThrowIf(IsDisposed, this);
			return TcpClient?.Client.LocalEndPoint as IPEndPoint;
		}
	}

	/// <summary>
	/// The underlying TCP client used for the connection.
	/// </summary>
	protected TcpClient? TcpClient;

	/// <inheritdoc />
	public virtual async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		ArgumentNullException.ThrowIfNull(local);
		ArgumentNullException.ThrowIfNull(dst);

		await CloseAsync(cancellationToken);

		TcpClient = new TcpClient(local) { NoDelay = true };
		await TcpClient.ConnectAsync(dst, cancellationToken);

		return TcpClient.Client.AsDuplexPipe();
	}

	/// <inheritdoc />
	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		CloseClient();

		return default;
	}

	/// <summary>
	/// Closes the underlying TCP client and releases its resources.
	/// </summary>
	protected virtual void CloseClient()
	{
		if (TcpClient is null)
		{
			return;
		}

		try
		{
			TcpClient.Client.Close(0);
		}
		finally
		{
			TcpClient.Dispose();
			TcpClient = default;
		}
	}

	/// <summary>
	/// Gets a value indicating whether this proxy has been disposed.
	/// </summary>
	public bool IsDisposed { get; private set; }

	/// <inheritdoc />
	public void Dispose()
	{
		IsDisposed = true;

		CloseClient();

		GC.SuppressFinalize(this);
	}
}
