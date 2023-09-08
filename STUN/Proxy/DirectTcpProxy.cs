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
			return _tcpClient?.Client.LocalEndPoint as IPEndPoint;
		}
	}

	private TcpClient? _tcpClient;

	public async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);
		Requires.NotNull(local, nameof(local));
		Requires.NotNull(dst, nameof(dst));

		await CloseAsync(cancellationToken);

		_tcpClient = new TcpClient(local) { NoDelay = true };
		await _tcpClient.ConnectAsync(dst, cancellationToken);

		return _tcpClient.Client.AsDuplexPipe();
	}

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);

		if (_tcpClient is not null)
		{
			CloseClient();
			_tcpClient = default;
		}

		return default;
	}

	private void CloseClient()
	{
		if (_tcpClient is null)
		{
			return;
		}

		_tcpClient.Client.Close(0);
		_tcpClient.Dispose();
	}

	public bool IsDisposed { get; private set; }

	public void Dispose()
	{
		IsDisposed = true;

		CloseClient();

		GC.SuppressFinalize(this);
	}
}
