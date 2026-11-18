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
	/// <remarks>
	/// Each call discards the current DTLS session and performs a new handshake with <paramref name="remoteEP"/>
	/// over the same inner proxy, so that every request reaches the endpoint it is addressed to.
	/// </remarks>
	public async ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
	{
		if (remoteEP is not IPEndPoint remote)
		{
			ThrowNotSupportedException();
		}

		// 旧会话只释放、不发 close_notify：DTLS 1.2 对端会回复 close_notify，回复会落入新会话的握手并使其失败
		DisposeDtlsTransport();

		UdpProxyDatagramTransport datagram = new(innerProxy, remote);

		DtlsClientOptions options = new()
		{
			ServerName = serverName,
			RemoteCertificateValidation = skipCertificateValidation
				? static (_, _, _) => true
				: default
		};

		DtlsTransport transport = await DtlsTransport.CreateClientAsync(datagram, options, cancellationToken);

		try
		{
			await transport.HandshakeAsync(cancellationToken);
		}
		catch
		{
			transport.Dispose();
			throw;
		}

		_dtlsSession = (transport, datagram);

		await transport.SendAsync(buffer, cancellationToken);
		return buffer.Length;

		static void ThrowNotSupportedException()
		{
			throw new NotSupportedException();
		}
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
