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
	private (DtlsTransport Transport, UdpProxyDatagramTransport Datagram)? _dtlsSession;

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
		if (_dtlsSession is not { Transport: var transport })
		{
			UdpProxyDatagramTransport datagram = new(innerProxy, (IPEndPoint)remoteEP);

			DtlsClientOptions options = new()
			{
				ServerName = serverName,
				RemoteCertificateValidation = skipCertificateValidation
					? static (_, _, _) => true
					: default
			};

			transport = await DtlsTransport.CreateClientAsync(datagram, options, cancellationToken);
			_dtlsSession = (transport, datagram);
			await transport.HandshakeAsync(cancellationToken);
		}

		await transport.SendAsync(buffer, cancellationToken);
		return buffer.Length;
	}

	/// <inheritdoc />
	public async ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
	{
		(DtlsTransport session, UdpProxyDatagramTransport datagram) = _dtlsSession ?? throw new InvalidOperationException("DTLS session has not been established.");

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
		if (_dtlsSession is not { Transport: var session })
		{
			return;
		}

		_dtlsSession = null;

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
		if (_dtlsSession is not { Transport: var session })
		{
			return;
		}

		_dtlsSession = null;
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
