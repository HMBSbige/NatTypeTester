using DTLS.Common;
using DTLS.Dtls;
using System.Net;
using System.Net.Sockets;

namespace STUN.Proxy;

/// <summary>
/// A UDP proxy that wraps an inner <see cref="IUdpProxy"/> with DTLS encryption.
/// </summary>
/// <param name="innerProxy">The inner UDP proxy to wrap with DTLS.</param>
/// <param name="serverName">The target server name for DTLS handshake.</param>
/// <param name="skipCertificateValidation">Whether to skip server certificate validation.</param>
public class DtlsProxy(IUdpProxy innerProxy, string serverName, bool skipCertificateValidation = false) : IUdpProxy
{
	private DtlsTransport? _dtlsTransport;

	/// <inheritdoc />
	public Socket Client => innerProxy.Client;

	/// <inheritdoc />
	public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
	{
		return innerProxy.ConnectAsync(cancellationToken);
	}

	/// <inheritdoc />
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
						CertificateChainDisposer.DisposeContents(chain);
						return true;
					}
				: default
			};

			_dtlsTransport = await DtlsTransport.CreateClientAsync(datagram, options);
			await _dtlsTransport.HandshakeAsync(cancellationToken);
		}

		await _dtlsTransport.SendAsync(buffer, cancellationToken);
		return buffer.Length;
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	public async ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			await CloseDtlsTransportAsync(cancellationToken);
		}
		finally
		{
			await innerProxy.CloseAsync(cancellationToken);
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		try
		{
			await CloseDtlsTransportAsync();
		}
		finally
		{
			await innerProxy.DisposeAsync();
			GC.SuppressFinalize(this);
		}
	}

	/// <inheritdoc />
	public void Dispose()
	{
		try
		{
			DisposeDtlsTransport();
		}
		finally
		{
			innerProxy.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	private async ValueTask CloseDtlsTransportAsync(CancellationToken cancellationToken = default)
	{
		if (_dtlsTransport is not { } session)
		{
			return;
		}

		_dtlsTransport = null;

		try
		{
			await session.CloseAsync(cancellationToken);
		}
		finally
		{
			await session.DisposeAsync();
		}
	}

	private void DisposeDtlsTransport()
	{
		if (_dtlsTransport is not { } session)
		{
			return;
		}

		_dtlsTransport = null;
		session.Dispose();
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
