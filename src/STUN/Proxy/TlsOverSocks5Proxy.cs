using Pipelines.Extensions;
using Socks5.Models;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace STUN.Proxy;

public class TlsOverSocks5Proxy(Socks5CreateOption socks5Options, string targetHost, bool skipCertificateValidation = false) : Socks5TcpProxy(socks5Options)
{
	private SslStream? _tlsStream;

	public override async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		IDuplexPipe pipe = await base.ConnectAsync(local, dst, cancellationToken);

		_tlsStream = new SslStream(pipe.AsStream(true));

		SslClientAuthenticationOptions sslOptions = new()
		{
			TargetHost = targetHost,
			RemoteCertificateValidationCallback = skipCertificateValidation
				? static (_, _, chain, _) =>
				{
					DisposeChainContents(chain);
					return true;
				}
			: default
		};

		await _tlsStream.AuthenticateAsClientAsync(sslOptions, cancellationToken);

		return _tlsStream.AsDuplexPipe();
	}

	protected override void CloseClient()
	{
		_tlsStream?.Dispose();
		base.CloseClient();
	}

	private static void DisposeChainContents(X509Chain? chain)
	{
		if (chain is null)
		{
			return;
		}

		foreach (X509Certificate2 extraCert in chain.ChainPolicy.ExtraStore)
		{
			extraCert.Dispose();
		}

		foreach (X509ChainElement element in chain.ChainElements)
		{
			element.Certificate.Dispose();
		}
	}
}
