using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Proxy
{
	public interface IUdpProxy : IDisposable
	{
		Socket Client { get; }
		ValueTask ConnectAsync(CancellationToken cancellationToken = default);
		ValueTask CloseAsync(CancellationToken cancellationToken = default);
		ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default);
		ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default);
	}
}
