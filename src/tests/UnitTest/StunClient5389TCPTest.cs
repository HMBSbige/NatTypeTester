using Dns.Net.Clients;
using Moq;
using Moq.Protected;
using Shouldly;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using System.Net;

namespace UnitTest;

public class StunClient5389TCPTest : TestBase
{
	private readonly DefaultDnsClient _dnsClient = new();

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);
	private static readonly IPEndPoint LocalAddress1 = IPEndPoint.Parse(@"127.0.0.1:114");
	private static readonly IPEndPoint MappedAddress1 = IPEndPoint.Parse(@"1.1.1.1:114");
	private static readonly IPEndPoint MappedAddress2 = IPEndPoint.Parse(@"1.1.1.1:514");
	private static readonly IPEndPoint ServerAddress = IPEndPoint.Parse(@"2.2.2.2:1919");
	private static readonly IPEndPoint ChangedAddress1 = IPEndPoint.Parse(@"3.3.3.3:23333");
	private static readonly IPEndPoint ChangedAddress2 = IPEndPoint.Parse(@"2.2.2.2:810");
	private static readonly IPEndPoint ChangedAddress3 = IPEndPoint.Parse(@"3.3.3.3:1919");

	[Fact]
	public async Task BindingTestSuccessAsync()
	{
		IPAddress ip = await _dnsClient.QueryAsync(@"stun.hot-chilli.net", CancellationToken);
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync(CancellationToken);

		response.BindingTestResult.ShouldBe(BindingTestResult.Success);
		response.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		response.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		response.PublicEndPoint.ShouldNotBeNull();
		response.LocalEndPoint.ShouldNotBeNull();
		response.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task BindingTestFailAsync()
	{
		IPAddress ip = IPAddress.Parse(@"1.1.1.1");
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync(CancellationToken);

		response.BindingTestResult.ShouldBe(BindingTestResult.Fail);
		response.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		response.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		response.PublicEndPoint.ShouldBeNull();
		response.LocalEndPoint.ShouldBeNull();
		response.OtherEndPoint.ShouldBeNull();
	}

	[Fact(Skip = "Dev test", SkipWhen = nameof(TestEnvironment.IsRunningOnGitHubActions), SkipType = typeof(TestEnvironment))]
	public async Task TlsBindingTestSuccessAsync()
	{
		StunServer.TryParse(@"stun.fitauto.ru", out StunServer? stunServer, StunServer.DefaultTlsPort).ShouldBeTrue();
		stunServer.ShouldNotBeNull();
		IPAddress ip = await _dnsClient.QueryAsync(stunServer.Hostname, CancellationToken);
		ITcpProxy tls = new TlsProxy(stunServer.Hostname);
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any, tls);

		StunResult5389 response = await client.BindingTestAsync(CancellationToken);

		response.BindingTestResult.ShouldBe(BindingTestResult.Success);
		response.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		response.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		response.PublicEndPoint.ShouldNotBeNull();
		response.LocalEndPoint.ShouldNotBeNull();
		response.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact(Explicit = true)]
	public async Task TestServerAsync()
	{
		const string url = @"https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts_tcp.txt";
		HttpClient httpClient = new();
		string listRaw = await httpClient.GetStringAsync(url, CancellationToken);
		string[] list = listRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (string host in list)
		{
			try
			{
				if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, StunServer.DefaultPort))
				{
					continue;
				}

				IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname, CancellationToken);
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, hostEndpoint.Port), Any);

				await client.QueryAsync(CancellationToken);

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

	[Fact(Explicit = true)]
	public async Task TestTlsServerAsync()
	{
		const string url = @"https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts_tcp.txt";
		HttpClient httpClient = new();
		string listRaw = await httpClient.GetStringAsync(url, CancellationToken);
		string[] list = listRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (string host in list)
		{
			try
			{
				if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, StunServer.DefaultTlsPort))
				{
					continue;
				}

				IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname, CancellationToken);
				ITcpProxy proxy = new TlsProxy(hostEndpoint.Hostname);
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultTlsPort), Any, proxy);

				await client.QueryAsync(CancellationToken);

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

	[Fact]
	public async Task MappingBehaviorTestFailAsync()
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(fail);

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Fail);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.None);
		client.State.PublicEndPoint.ShouldBeNull();
		client.State.LocalEndPoint.ShouldBeNull();
		client.State.OtherEndPoint.ShouldBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestUnsupportedServerAsync()
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		await TestAsync();

		StunResult5389 r2 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress2
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);
		await TestAsync();

		StunResult5389 r3 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress3
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r3);
		await TestAsync();

		return;

		async Task TestAsync()
		{
			await client.QueryAsync(CancellationToken);

			client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
			client.State.MappingBehavior.ShouldBe(MappingBehavior.UnsupportedServer);
			client.State.FilteringBehavior.ShouldBe(FilteringBehavior.None);
			client.State.PublicEndPoint.ShouldNotBeNull();
			client.State.LocalEndPoint.ShouldNotBeNull();
		}
	}

	[Fact]
	public async Task MappingBehaviorTestDirectAsync()
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

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(response);

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Direct);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.None);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestEndpointIndependentAsync()
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
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.EndpointIndependent);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.None);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTest2FailAsync()
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

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ServerAddress, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress3, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);
		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Fail);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.None);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestAddressDependentAsync()
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

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ServerAddress, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress3, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress1, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r3);

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.AddressDependent);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.None);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestAddressAndPortDependentAsync()
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

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ServerAddress, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress3, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress1, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r3);

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.AddressAndPortDependent);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.None);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTest3FailAsync()
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

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ServerAddress, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress3, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress1, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r3);

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Fail);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.None);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task FilteringBehaviorTestAsync()
	{
		Mock<StunClient5389TCP> mock = new(ServerAddress, Any, default!, true);
		IStunClient5389 client = mock.Object;

		await Should.ThrowAsync<NotSupportedException>(async () => await client.FilteringBehaviorTestAsync(CancellationToken));
	}
}
