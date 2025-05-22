using Pipelines.Extensions;
using Socks5.Models;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;

namespace STUN.Proxy;

public class TlsOverSocks5Proxy : Socks5TcpProxy
{
	private SslStream? _tlsStream;

	private readonly string _targetHost;

	public TlsOverSocks5Proxy(Socks5CreateOption socks5Options, string targetHost) : base(socks5Options)
	{
		_targetHost = targetHost;
	}

	public override async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		IDuplexPipe pipe = await base.ConnectAsync(local, dst, cancellationToken);

		_tlsStream = new SslStream(pipe.AsStream(true));

		await _tlsStream.AuthenticateAsClientAsync(_targetHost);

		return _tlsStream.AsDuplexPipe();
	}

	protected override void CloseClient()
	{
		_tlsStream?.Dispose();
		base.CloseClient();
	}
}
