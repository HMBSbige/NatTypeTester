using Dns.Net.Clients;
using Moq;
using Moq.Protected;
using Shouldly;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using System.Net;

namespace UnitTest;

public class StunClien5389UDPTest : TestBase
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

	private static readonly StunMessage5389 DefaultStunMessage = new();

	[Fact]
	public async Task BindingTestSuccessAsync()
	{
		IPAddress ip = await _dnsClient.QueryAsync(@"stun.hot-chilli.net", CancellationToken);
		using StunClient5389UDP client = new(new IPEndPoint(ip, StunServer.DefaultPort), Any);

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
		using StunClient5389UDP client = new(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync(CancellationToken);

		response.BindingTestResult.ShouldBe(BindingTestResult.Fail);
		response.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		response.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		response.PublicEndPoint.ShouldBeNull();
		response.LocalEndPoint.ShouldBeNull();
		response.OtherEndPoint.ShouldBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestFailAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(fail);

		await client.MappingBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Fail);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldBeNull();
		client.State.LocalEndPoint.ShouldBeNull();
		client.State.OtherEndPoint.ShouldBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestUnsupportedServerAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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
			await client.MappingBehaviorTestAsync(CancellationToken);

			client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
			client.State.MappingBehavior.ShouldBe(MappingBehavior.UnsupportedServer);
			client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
			client.State.PublicEndPoint.ShouldNotBeNull();
			client.State.LocalEndPoint.ShouldNotBeNull();
		}
	}

	[Fact]
	public async Task MappingBehaviorTestDirectAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 response = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = MappedAddress1,
			OtherEndPoint = ChangedAddress1
		};

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(response);

		await client.MappingBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Direct);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestEndpointIndependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		await client.MappingBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.EndpointIndependent);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTest2FailAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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
		await client.MappingBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Fail);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestAddressDependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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

		await client.MappingBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.AddressDependent);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTestAddressAndPortDependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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

		await client.MappingBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.AddressAndPortDependent);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task MappingBehaviorTest3FailAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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

		await client.MappingBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Fail);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task FilteringBehaviorTestFailAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(fail);

		await client.FilteringBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Fail);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldBeNull();
		client.State.LocalEndPoint.ShouldBeNull();
		client.State.OtherEndPoint.ShouldBeNull();
	}

	[Fact]
	public async Task FilteringBehaviorTestUnsupportedServerAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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
			await client.FilteringBehaviorTestAsync(CancellationToken);

			client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
			client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
			client.State.FilteringBehavior.ShouldBe(FilteringBehavior.UnsupportedServer);
			client.State.PublicEndPoint.ShouldNotBeNull();
			client.State.LocalEndPoint.ShouldNotBeNull();
		}
	}

	[Fact]
	public async Task FilteringBehaviorTestEndpointIndependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResponse r2 = new(DefaultStunMessage, ChangedAddress1, LocalAddress1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest2Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);

		await client.FilteringBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.EndpointIndependent);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task FilteringBehaviorTest2UnsupportedServerAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResponse r2 = new(DefaultStunMessage, ServerAddress, LocalAddress1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest2Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);

		await client.FilteringBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.UnsupportedServer);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task FilteringBehaviorTestAddressAndPortDependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest2Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest3Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));

		await client.FilteringBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.AddressAndPortDependent);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task FilteringBehaviorTestAddressDependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResponse r3 = new(DefaultStunMessage, ChangedAddress2, LocalAddress1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest2Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest3Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r3);

		await client.FilteringBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.AddressDependent);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task FilteringBehaviorTest3UnsupportedServerAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		StunResponse r3 = new(DefaultStunMessage, ServerAddress, LocalAddress1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest2Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest3Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r3);

		await client.FilteringBehaviorTestAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.UnsupportedServer);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task QueryFailTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(fail);

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Fail);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.Unknown);
		client.State.PublicEndPoint.ShouldBeNull();
		client.State.LocalEndPoint.ShouldBeNull();
		client.State.OtherEndPoint.ShouldBeNull();
	}

	[Fact]
	public async Task QueryUnsupportedServerTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ServerAddress
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Unknown);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.UnsupportedServer);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task QueryMappingBehaviorDirectTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = MappedAddress1,
			OtherEndPoint = ChangedAddress1
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ServerAddress, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest2Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest3Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.Direct);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.AddressAndPortDependent);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task QueryMappingBehaviorEndpointIndependentTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest2Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest3Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.EndpointIndependent);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.AddressAndPortDependent);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}

	[Fact]
	public async Task QueryMappingBehaviorAddressAndPortDependentTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest2Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));
		mock.Protected().Setup<ValueTask<StunResponse?>>(@"FilteringBehaviorTest3Async", ItExpr.IsAny<CancellationToken>()).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(CancellationToken);

		client.State.BindingTestResult.ShouldBe(BindingTestResult.Success);
		client.State.MappingBehavior.ShouldBe(MappingBehavior.AddressAndPortDependent);
		client.State.FilteringBehavior.ShouldBe(FilteringBehavior.AddressAndPortDependent);
		client.State.PublicEndPoint.ShouldNotBeNull();
		client.State.LocalEndPoint.ShouldNotBeNull();
		client.State.OtherEndPoint.ShouldNotBeNull();
	}
}
