using Pipelines.Extensions;
using System.IO.Pipelines;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace STUN.Proxy;

/// <summary>
/// A TCP proxy that establishes a direct TLS-encrypted connection to the destination.
/// </summary>
/// <param name="targetHost">The target host name for TLS server name indication.</param>
/// <param name="skipCertificateValidation">Whether to skip server certificate validation.</param>
public class TlsProxy(string targetHost, bool skipCertificateValidation = false) : DirectTcpProxy
{
	private SslStream? _tlsStream;

	/// <inheritdoc />
	public override async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		ArgumentNullException.ThrowIfNull(local);
		ArgumentNullException.ThrowIfNull(dst);

		await CloseAsync(cancellationToken);

		TcpClient = new TcpClient(local) { NoDelay = true };
		await TcpClient.ConnectAsync(dst, cancellationToken);

		_tlsStream = new SslStream(TcpClient.GetStream(), true);

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

	/// <inheritdoc />
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
