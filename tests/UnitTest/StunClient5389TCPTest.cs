using Dns.Net.Clients;
using Moq;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using System.Net;

namespace UnitTest;

public class StunClient5389TCPTest : TestBase
{
	private readonly DefaultAClient _dnsClient = new();

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);
	private static readonly IPEndPoint LocalAddress1 = IPEndPoint.Parse(@"127.0.0.1:114");
	private static readonly IPEndPoint MappedAddress1 = IPEndPoint.Parse(@"1.1.1.1:114");
	private static readonly IPEndPoint MappedAddress2 = IPEndPoint.Parse(@"1.1.1.1:514");
	private static readonly IPEndPoint ServerAddress = IPEndPoint.Parse(@"2.2.2.2:1919");
	private static readonly IPEndPoint ChangedAddress1 = IPEndPoint.Parse(@"3.3.3.3:23333");
	private static readonly IPEndPoint ChangedAddress2 = IPEndPoint.Parse(@"2.2.2.2:810");
	private static readonly IPEndPoint ChangedAddress3 = IPEndPoint.Parse(@"3.3.3.3:1919");

	[Test]
	public async Task BindingTestSuccessAsync(CancellationToken cancellationToken)
	{
		IPAddress ip = await _dnsClient.QueryAsync(@"stun.hot-chilli.net", cancellationToken);
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any);

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

		await Assert.That(response.BindingTestResult).IsEqualTo(BindingTestResult.Fail);
		await Assert.That(response.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(response.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(response.PublicEndPoint).IsNull();
		await Assert.That(response.LocalEndPoint).IsNull();
		await Assert.That(response.OtherEndPoint).IsNull();
	}

	[Test]
	[Skip("Dev test")]
	public async Task TlsBindingTestSuccessAsync(CancellationToken cancellationToken)
	{
		await Assert.That(StunServer.TryParse(@"stun.fitauto.ru", out StunServer? stunServer, StunServer.DefaultTlsPort)).IsTrue();
		await Assert.That(stunServer).IsNotNull();
		IPAddress ip = await _dnsClient.QueryAsync(stunServer!.Hostname, cancellationToken);
		ITcpProxy tls = new TlsProxy(stunServer.Hostname);
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any, tls);

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(response.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(response.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(response.PublicEndPoint).IsNotNull();
		await Assert.That(response.LocalEndPoint).IsNotNull();
		await Assert.That(response.OtherEndPoint).IsNotNull();
	}

	[Test]
	[Explicit]
	public async Task TestServerAsync(CancellationToken cancellationToken)
	{
		const string url = @"https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts_tcp.txt";
		HttpClient httpClient = new();
		string listRaw = await httpClient.GetStringAsync(url, cancellationToken);
		string[] list = listRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (string host in list)
		{
			try
			{
				if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, StunServer.DefaultPort))
				{
					continue;
				}

				IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname, cancellationToken);
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, hostEndpoint.Port), Any);

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
	[Explicit]
	public async Task TestTlsServerAsync(CancellationToken cancellationToken)
	{
		const string url = @"https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts_tcp.txt";
		HttpClient httpClient = new();
		string listRaw = await httpClient.GetStringAsync(url, cancellationToken);
		string[] list = listRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (string host in list)
		{
			try
			{
				if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, StunServer.DefaultTlsPort))
				{
					continue;
				}

				IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname, cancellationToken);
				ITcpProxy proxy = new TlsProxy(hostEndpoint.Hostname);
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultTlsPort), Any, proxy);

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
	public async Task MappingBehaviorTestFailAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fail);

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Fail);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.None);
		await Assert.That(client.State.PublicEndPoint).IsNull();
		await Assert.That(client.State.LocalEndPoint).IsNull();
		await Assert.That(client.State.OtherEndPoint).IsNull();
	}

	[Test]
	public async Task MappingBehaviorTestUnsupportedServerAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1
		};
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		await TestAsync();

		StunResult5389 r2 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress2
		};
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		await TestAsync();

		StunResult5389 r3 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress3
		};
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r3);
		await TestAsync();

		return;

		async Task TestAsync()
		{
			await client.QueryAsync(cancellationToken);

			await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
			await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.UnsupportedServer);
			await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.None);
			await Assert.That(client.State.PublicEndPoint).IsNotNull();
			await Assert.That(client.State.LocalEndPoint).IsNotNull();
		}
	}

	[Test]
	public async Task MappingBehaviorTestDirectAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 response = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = MappedAddress1,
			OtherEndPoint = ChangedAddress1
		};

		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Direct);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.None);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTestEndpointIndependentAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.EndpointIndependent);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.None);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTest2FailAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResult5389 r2 = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Fail);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.None);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTestAddressDependentAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResult5389 r2 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress2,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResult5389 r3 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress2,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};

		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress1, It.IsAny<CancellationToken>())).ReturnsAsync(r3);

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.AddressDependent);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.None);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTestAddressAndPortDependentAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResult5389 r2 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress2,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResult5389 r3 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};

		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress1, It.IsAny<CancellationToken>())).ReturnsAsync(r3);

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.AddressAndPortDependent);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.None);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTest3FailAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResult5389 r2 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress2,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResult5389 r3 = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress1, It.IsAny<CancellationToken>())).ReturnsAsync(r3);

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Fail);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.None);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task FilteringBehaviorTestAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		await Assert.That(async () => await client.FilteringBehaviorTestAsync(cancellationToken)).Throws<NotSupportedException>();
	}
}
