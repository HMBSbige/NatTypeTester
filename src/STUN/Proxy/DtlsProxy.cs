using DTLS.Common;
using DTLS.Dtls;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace STUN.Proxy;

public class DtlsProxy(IUdpProxy innerProxy, string serverName, bool skipCertificateValidation = false) : IUdpProxy
{
	private DtlsTransport? _dtlsTransport;

	private static readonly Lazy<X509Certificate2Collection> AndroidRootCertificates = new(LoadAndroidRootCertificates);

	public Socket Client => innerProxy.Client;

	public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
	{
		return innerProxy.ConnectAsync(cancellationToken);
	}

	public async ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
	{
		if (_dtlsTransport is null)
		{
			UdpProxyDatagramTransport datagram = new(innerProxy, (IPEndPoint)remoteEP);

			DtlsClientOptions options = new()
			{
				ServerName = serverName,
				RemoteCertificateValidation = skipCertificateValidation
					? static (_, chain, _) =>
					{
						DisposeChainContents(chain);
						return true;
					}
				: OperatingSystem.IsAndroid()
						? ValidateCertificateOnAndroid
						: null
			};

			_dtlsTransport = await DtlsTransport.CreateClientAsync(datagram, options);
			await _dtlsTransport.HandshakeAsync(cancellationToken);
		}

		await _dtlsTransport.SendAsync(buffer, cancellationToken);
		return buffer.Length;
	}

	public async ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
	{
		DtlsTransport session = _dtlsTransport ?? throw new InvalidOperationException("DTLS session has not been established.");
		UdpProxyDatagramTransport datagram = (UdpProxyDatagramTransport)session.InnerTransport;

		int received = await session.ReceiveAsync(buffer, cancellationToken);

		return new SocketReceiveMessageFromResult
		{
			ReceivedBytes = received,
			RemoteEndPoint = datagram.LastRemoteEndPoint ?? throw new InvalidOperationException("No remote endpoint received."),
			PacketInformation = datagram.LastPacketInformation
		};
	}

	public async ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		if (_dtlsTransport is { } session)
		{
			await session.DisposeAsync();
			_dtlsTransport = null;
		}

		await innerProxy.CloseAsync(cancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		if (_dtlsTransport is { } session)
		{
			await session.DisposeAsync();
			_dtlsTransport = null;
		}

		await innerProxy.DisposeAsync();
		GC.SuppressFinalize(this);
	}

	public void Dispose()
	{
		if (_dtlsTransport is { } session)
		{
			session.Dispose();
			_dtlsTransport = null;
		}

		innerProxy.Dispose();
		GC.SuppressFinalize(this);
	}

	private static bool ValidateCertificateOnAndroid(X509Certificate2? cert, X509Chain? chain, SslPolicyErrors errors)
	{
		// https://github.com/dotnet/runtime/issues/84202
		// workaround
		try
		{
			if (cert is null)
			{
				return false;
			}

			if (errors.HasFlag(SslPolicyErrors.RemoteCertificateChainErrors))
			{
				errors &= ~SslPolicyErrors.RemoteCertificateChainErrors;

				X509Chain androidChain = new();

				try
				{
					androidChain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
					androidChain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
					androidChain.ChainPolicy.CustomTrustStore.AddRange(AndroidRootCertificates.Value);

					if (chain is not null)
					{
						foreach (X509Certificate2 extraCert in chain.ChainPolicy.ExtraStore)
						{
							androidChain.ChainPolicy.ExtraStore.Add(extraCert);
						}
					}

					if (!androidChain.Build(cert))
					{
						errors |= SslPolicyErrors.RemoteCertificateChainErrors;
					}
				}
				finally
				{
					foreach (X509ChainElement element in androidChain.ChainElements)
					{
						element.Certificate.Dispose();
					}

					androidChain.Dispose();
				}
			}

			return errors is SslPolicyErrors.None;
		}
		finally
		{
			// DTLS 库在有回调时不释放 chain 内容，由回调方负责
			DisposeChainContents(chain);
		}
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

	private static X509Certificate2Collection LoadAndroidRootCertificates()
	{
		X509Certificate2Collection certs = [];

		ReadOnlySpan<string> caDirs =
		[
			"/apex/com.android.conscrypt/cacerts",
			"/system/etc/security/cacerts"
		];

		foreach (string dir in caDirs)
		{
			if (!Directory.Exists(dir))
			{
				continue;
			}

			foreach (string file in Directory.GetFiles(dir))
			{
				try
				{
					certs.Add(X509CertificateLoader.LoadCertificateFromFile(file));
				}
				catch
				{
					// Skip invalid cert files
				}
			}

			break;
		}

		return certs;
	}

	private sealed class UdpProxyDatagramTransport(IUdpProxy proxy, IPEndPoint peerEndPoint) : IDatagramTransport
	{
		public EndPoint? LastRemoteEndPoint { get; private set; }

		public IPPacketInformation LastPacketInformation { get; private set; }

		public async ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default)
		{
			await proxy.SendToAsync(datagram, SocketFlags.None, peerEndPoint, cancellationToken);
		}

		public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			SocketReceiveMessageFromResult result = await proxy.ReceiveMessageFromAsync(buffer, SocketFlags.None, peerEndPoint, cancellationToken);
			LastRemoteEndPoint = result.RemoteEndPoint;
			LastPacketInformation = result.PacketInformation;
			return result.ReceivedBytes;
		}
	}
}
