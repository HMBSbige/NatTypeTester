using Microsoft;
using Pipelines.Extensions;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;

namespace STUN.Proxy;

public class TlsProxy : DirectTcpProxy
{
	private SslStream? _tlsStream;

	private readonly string _targetHost;

	public TlsProxy(string targetHost)
	{
		_targetHost = targetHost;
	}

	public override async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);
		Requires.NotNull(local, nameof(local));
		Requires.NotNull(dst, nameof(dst));

		await CloseAsync(cancellationToken);

		TcpClient = new TcpClient(local) { NoDelay = true };
		await TcpClient.ConnectAsync(dst, cancellationToken);

		_tlsStream = new SslStream(TcpClient.GetStream(), true);

		await _tlsStream.AuthenticateAsClientAsync(_targetHost);

		return _tlsStream.AsDuplexPipe();
	}

	protected override void CloseClient()
	{
		_tlsStream?.Dispose();
		base.CloseClient();
	}
}
