using Dns.Net.Clients;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using System.Diagnostics;
using System.Net;

namespace UnitTest;

public class StunClient5389UDPTest
{
	private readonly DefaultAClient _dnsClient = new();

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);

	private static readonly IPEndPoint Loopback = new(IPAddress.Loopback, 0);

	private static readonly IPEndPoint Mapped = IPEndPoint.Parse(@"203.0.113.5:40000");

	private static readonly TimeSpan ShortReceiveTimeout = TimeSpan.FromMilliseconds(500);

	[Test]
	public async Task BindingTestSuccessAsync(CancellationToken cancellationToken)
	{
		Skip.When(TestEnvironment.IsCI, "Skipped on CI");

		IPAddress ip = await _dnsClient.QueryAsync(TestEnvironment.StunServerHost, cancellationToken);
		await using StunClient5389UDP client = new(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(response.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(response.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(response.PublicEndPoint).IsNotNull();
		await Assert.That(response.LocalEndPoint).IsNotNull();
		await Assert.That(response.OtherEndPoint).IsNotNull();
	}

	[Test]
	[Timeout(10_000)]
	[Arguments(Interference.StrayResponse)]
	[Arguments(Interference.NotStun)]
	public async Task BindingTestSkipsDatagramsThatAreNotTheResponseAsync(Interference interference, CancellationToken cancellationToken)
	{
		await using UdpStunServer server = UdpStunServer.Responding(Mapped, interference);
		await using StunClient5389UDP client = new(server.EndPoint, Loopback);

		StunResult5389 result = await client.BindingTestAsync(cancellationToken);

		using (Assert.Multiple())
		{
			await Assert.That(result.BindingTestResult).IsEqualTo(BindingTestResult.Success);
			await Assert.That(result.PublicEndPoint).IsEqualTo(Mapped);
		}
	}

	[Test]
	[Timeout(10_000)]
	[Arguments(Interference.None)]
	[Arguments(Interference.StrayResponse)]
	public async Task BindingTestFailsAtReceiveTimeoutAsync(Interference interference, CancellationToken cancellationToken)
	{
		await using UdpStunServer server = UdpStunServer.Unresponsive(interference);
		await using StunClient5389UDP client = new(server.EndPoint, Loopback) { ReceiveTimeout = ShortReceiveTimeout };

		long start = Stopwatch.GetTimestamp();
		StunResult5389 result = await client.BindingTestAsync(cancellationToken);
		TimeSpan elapsed = Stopwatch.GetElapsedTime(start);

		using (Assert.Multiple())
		{
			await Assert.That(result).IsEqualTo(new StunResult5389
			{
				BindingTestResult = BindingTestResult.Fail
			});
			// The wait does not end early (with slack for coarse timers), and unrelated datagrams restarting it would trip [Timeout]
			await Assert.That(elapsed).IsGreaterThanOrEqualTo(ShortReceiveTimeout / 2);
		}
	}

	[Test]
	[Timeout(10_000)]
	public async Task BindingTestSkipsConnectionResetReportedByProxyAsync(CancellationToken cancellationToken)
	{
		await using UdpStunServer server = UdpStunServer.Responding(Mapped, Interference.None);
		await using StunClient5389UDP client = new(server.EndPoint, Loopback, new ConnectionResetOnceProxy(new NoneUdpProxy(Loopback)));

		StunResult5389 result = await client.BindingTestAsync(cancellationToken);

		using (Assert.Multiple())
		{
			await Assert.That(result.BindingTestResult).IsEqualTo(BindingTestResult.Success);
			await Assert.That(result.PublicEndPoint).IsEqualTo(Mapped);
		}
	}

	[Test]
	[Timeout(10_000)]
	public async Task BindingTestAcceptsResponseThatArrivesBeforeReceiveAsync(CancellationToken cancellationToken)
	{
		await using UdpStunServer server = UdpStunServer.Responding(Mapped, Interference.None);
		LateReceiveProxy proxy = new(new NoneUdpProxy(Loopback));
		await using StunClient5389UDP client = new(server.EndPoint, Loopback, proxy);

		StunResult5389 result = await client.BindingTestAsync(cancellationToken);

		using (Assert.Multiple())
		{
			await Assert.That(result.BindingTestResult).IsEqualTo(BindingTestResult.Success);
			await Assert.That(result.PublicEndPoint).IsEqualTo(Mapped);
			await Assert.That(result.LocalEndPoint).IsEqualTo(UdpSockets.GetLocalEndPoint(proxy.Client));
		}
	}
}
