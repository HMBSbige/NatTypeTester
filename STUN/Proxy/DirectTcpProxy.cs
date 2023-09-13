using Microsoft;
using Pipelines.Extensions;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace STUN.Proxy;

public class DirectTcpProxy : ITcpProxy, IDisposableObservable
{
	public IPEndPoint? CurrentLocalEndPoint
	{
		get
		{
			Verify.NotDisposed(this);
			return TcpClient?.Client.LocalEndPoint as IPEndPoint;
		}
	}

	protected TcpClient? TcpClient;

	public virtual async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);
		Requires.NotNull(local, nameof(local));
		Requires.NotNull(dst, nameof(dst));

		await CloseAsync(cancellationToken);

		TcpClient = new TcpClient(local) { NoDelay = true };
		await TcpClient.ConnectAsync(dst, cancellationToken);

		return TcpClient.Client.AsDuplexPipe();
	}

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);

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
