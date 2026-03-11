using DTLS.Common;
using DTLS.Dtls;
using System.Net;
using System.Net.Sockets;

namespace STUN.Proxy;

public class DtlsProxy(IUdpProxy innerProxy, string serverName) : IUdpProxy
{
	private DtlsTransport? _dtlsTransport;

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

			DtlsClientOptions options = new() { ServerName = serverName };

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
