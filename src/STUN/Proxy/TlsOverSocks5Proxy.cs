using Pipelines.Extensions;
using Socks5.Models;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;

namespace STUN.Proxy;

public class TlsOverSocks5Proxy(Socks5CreateOption socks5Options, string targetHost) : Socks5TcpProxy(socks5Options)
{
	private SslStream? _tlsStream;

	public override async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		IDuplexPipe pipe = await base.ConnectAsync(local, dst, cancellationToken);

		_tlsStream = new SslStream(pipe.AsStream(true));

		await _tlsStream.AuthenticateAsClientAsync(targetHost);

		return _tlsStream.AsDuplexPipe();
	}

	protected override void CloseClient()
	{
		_tlsStream?.Dispose();
		base.CloseClient();
	}
}
