using STUN.Messages;
using STUN.Proxy;
using System.Net;
using System.Net.Sockets;

namespace UnitTest;

public class NoneUdpProxyTest
{
	private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromMilliseconds(500);

	[Test]
	public async Task ReceiveIgnoresPortUnreachableAsync(CancellationToken cancellationToken)
	{
		IPEndPoint closed = UdpSockets.GetClosedEndPoint();
		await using NoneUdpProxy proxy = new(new IPEndPoint(IPAddress.Loopback, 0));

		await proxy.SendToAsync(new byte[StunMessage5389.HeaderLength], SocketFlags.None, closed, cancellationToken);

		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(ReceiveTimeout);

		// By default, Windows reports ICMP port unreachable as ConnectionReset on the next receive;
		// on other platforms, an unconnected UDP socket just keeps waiting
		await Assert.That(async () => await proxy.ReceiveMessageFromAsync(new byte[0x10000], SocketFlags.None, closed, cts.Token)).Throws<OperationCanceledException>();
	}
}
