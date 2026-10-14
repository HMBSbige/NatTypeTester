using Dns.Net.Clients;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using System.Net;

namespace UnitTest;

public class StunClient5389TCPTest
{
	private readonly DefaultAClient _dnsClient = new();

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);

	[Test]
	[MatrixDataSource]
	public async Task BindingTestSuccessAsync([Matrix] bool useTls, CancellationToken cancellationToken)
	{
		Skip.When(TestEnvironment.IsCI, "Skipped on CI");

		IPAddress ip = await _dnsClient.QueryAsync(TestEnvironment.StunServerHost, cancellationToken);
		ushort port = useTls ? StunServer.DefaultTlsPort : StunServer.DefaultPort;
		ITcpProxy? proxy = useTls ? new TlsProxy(TestEnvironment.StunServerHost) : null;
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, port), Any, proxy);

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(response.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(response.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(response.PublicEndPoint).IsNotNull();
		await Assert.That(response.LocalEndPoint).IsNotNull();
		await Assert.That(response.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task BindingTestFailAsync(CancellationToken cancellationToken)
	{
		IPAddress ip = IPAddress.Parse(@"1.1.1.1");
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response).IsEqualTo(new StunResult5389
		{
			BindingTestResult = BindingTestResult.Fail
		});
	}

	[Test]
	[Explicit]
	[CombinedDataSources]
	public async Task TestServerAsync
	(
		[ClassDataSource<StunServerList>(Shared = SharedType.PerClass)]
		StunServerList serverList,
		[Arguments(false, true)] bool useTls,
		CancellationToken cancellationToken
	)
	{
		ushort defaultPort = useTls ? StunServer.DefaultTlsPort : StunServer.DefaultPort;

		foreach (string host in serverList.Hosts)
		{
			try
			{
				if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, defaultPort))
				{
					continue;
				}

				IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname, cancellationToken);
				ushort port = useTls ? StunServer.DefaultTlsPort : hostEndpoint.Port;
				ITcpProxy? proxy = useTls ? new TlsProxy(hostEndpoint.Hostname) : null;
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, port), Any, proxy);

				await client.QueryAsync(cancellationToken);

				if (client.State.MappingBehavior is MappingBehavior.AddressAndPortDependent or MappingBehavior.AddressDependent or MappingBehavior.EndpointIndependent or MappingBehavior.Direct)
				{
					Console.WriteLine(host);
				}
			}
			catch
			{
				// ignored
			}
		}
	}

	[Test]
	public async Task FilteringBehaviorTestAsync(CancellationToken cancellationToken)
	{
		await Assert.That
		(async () =>
			{
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(IPAddress.Loopback, StunServer.DefaultPort), Any);
				await client.FilteringBehaviorTestAsync(cancellationToken);
			}
		).Throws<NotSupportedException>();
	}
}
