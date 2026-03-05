using Pipelines.Extensions;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace STUN.Proxy;

public class DirectTcpProxy : ITcpProxy
{
	public IPEndPoint? CurrentLocalEndPoint
	{
		get
		{
			ObjectDisposedException.ThrowIf(IsDisposed, this);
			return TcpClient?.Client.LocalEndPoint as IPEndPoint;
		}
	}

	protected TcpClient? TcpClient;

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

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		CloseClient();

		return default;
	}

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

	public bool IsDisposed { get; private set; }

	public void Dispose()
	{
		IsDisposed = true;

		CloseClient();

		GC.SuppressFinalize(this);
	}
}
