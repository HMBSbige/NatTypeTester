using DTLS.Common;
using DTLS.Dtls;
using STUN.Enums;
using STUN.Messages;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using static STUN.Utils.AttributeExtensions;

namespace UnitTest;

internal sealed class DtlsStunServer : IAsyncDisposable
{
	private readonly Socket _socket;
	private readonly CancellationTokenSource _cts = new();
	private readonly Task _serveTask;
	private int _requestCount;

	public IPEndPoint EndPoint { get; }

	public int RequestCount => Volatile.Read(ref _requestCount);

	/// <summary>
	/// Starts the server on the bound <paramref name="socket"/> and takes ownership of it.
	/// </summary>
	public DtlsStunServer(Socket socket, X509Certificate2 certificate, IPEndPoint mappedAddress, IPEndPoint? otherAddress = null, DtlsVersion? maxVersion = null)
	{
		_socket = socket;
		EndPoint = UdpSockets.GetLocalEndPoint(socket);

		DtlsServerOptions options = new()
		{
			Certificate = certificate,
			MaxVersion = maxVersion,
			HandshakeTimeout = Timeout.InfiniteTimeSpan
		};

		StunAttribute mapping = BuildMapping(IpFamily.IPv4, mappedAddress.Address, (ushort)mappedAddress.Port);
		StunAttribute[] attributes = otherAddress is null
			? [mapping]
			: [mapping, BuildChangeAddress(IpFamily.IPv4, otherAddress.Address, (ushort)otherAddress.Port)];

		_serveTask = Task.Run(() => ServeAsync(options, attributes, _cts.Token));
	}

	/// <summary>
	/// The DTLS server requires the certificate to have an exportable ECDSA private key.
	/// </summary>
	public static X509Certificate2 CreateCertificate()
	{
		const string password = @"password";

		using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
		CertificateRequest request = new(@"CN=localhost", key, HashAlgorithmName.SHA256);
		using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));

		return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pkcs12, password), password, X509KeyStorageFlags.Exportable);
	}

	private async Task ServeAsync(DtlsServerOptions options, StunAttribute[] attributes, CancellationToken cancellationToken)
	{
		using DtlsTransport server = await DtlsTransport.CreateServerAsync(new SocketDatagramTransport(_socket), options, cancellationToken);
		await server.HandshakeAsync(cancellationToken);

		byte[] buffer = new byte[0x10000];

		while (true)
		{
			int length = await server.ReceiveAsync(buffer, cancellationToken);

			if (length is 0)
			{
				// The underlying transport has been closed
				return;
			}

			StunMessage5389 request = new();

			if (!request.TryParse(buffer.AsMemory(0, length)))
			{
				continue;
			}

			Interlocked.Increment(ref _requestCount);

			StunMessage5389 response = new()
			{
				StunMessageType = StunMessageType.BindingResponse,
				Attributes = attributes
			};
			request.TransactionId.CopyTo(response.TransactionId);

			length = response.WriteTo(buffer);
			await server.SendAsync(buffer.AsMemory(0, length), cancellationToken);
		}
	}

	public async ValueTask DisposeAsync()
	{
		await _cts.CancelAsync();

		try
		{
			await _serveTask;
		}
		catch (OperationCanceledException)
		{
			// Stopped normally
		}
		catch (SocketException)
		{
			// On Windows, once the client socket is closed, ICMP port unreachable for the server's retransmissions
			// makes receiving throw ConnectionReset
		}
		finally
		{
			_socket.Dispose();
			_cts.Dispose();
		}
	}

	private sealed class SocketDatagramTransport(Socket socket) : IDatagramTransport
	{
		private static readonly IPEndPoint Any = new(IPAddress.Any, 0);

		private EndPoint? _peer;

		public async ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
		{
			SocketReceiveFromResult result = await socket.ReceiveFromAsync(buffer, SocketFlags.None, Any, cancellationToken);
			_peer = result.RemoteEndPoint;
			return result.ReceivedBytes;
		}

		public async ValueTask SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default)
		{
			EndPoint peer = _peer ?? throw new InvalidOperationException(@"No datagram has been received yet.");
			await socket.SendToAsync(datagram, SocketFlags.None, peer, cancellationToken);
		}
	}
}
