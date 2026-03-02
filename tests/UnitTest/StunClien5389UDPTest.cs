using Dns.Net.Clients;
using Moq;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using System.Net;

namespace UnitTest;

public class StunClien5389UDPTest : TestBase
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

	private static readonly StunMessage5389 DefaultStunMessage = new();

	[Test]
	public async Task BindingTestSuccessAsync(CancellationToken cancellationToken)
	{
		IPAddress ip = await _dnsClient.QueryAsync(@"stun.hot-chilli.net", cancellationToken);
		using StunClient5389UDP client = new(new IPEndPoint(ip, StunServer.DefaultPort), Any);

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
		using StunClient5389UDP client = new(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response.BindingTestResult).IsEqualTo(BindingTestResult.Fail);
		await Assert.That(response.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(response.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(response.PublicEndPoint).IsNull();
		await Assert.That(response.LocalEndPoint).IsNull();
		await Assert.That(response.OtherEndPoint).IsNull();
	}

	[Test]
	public async Task MappingBehaviorTestFailAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fail);

		await client.MappingBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Fail);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNull();
		await Assert.That(client.State.LocalEndPoint).IsNull();
		await Assert.That(client.State.OtherEndPoint).IsNull();
	}

	[Test]
	public async Task MappingBehaviorTestUnsupportedServerAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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
			await client.MappingBehaviorTestAsync(cancellationToken);

			await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
			await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.UnsupportedServer);
			await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
			await Assert.That(client.State.PublicEndPoint).IsNotNull();
			await Assert.That(client.State.LocalEndPoint).IsNotNull();
		}
	}

	[Test]
	public async Task MappingBehaviorTestDirectAsync(CancellationToken cancellationToken)
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

		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

		await client.MappingBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Direct);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTestEndpointIndependentAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		await client.MappingBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.EndpointIndependent);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTest2FailAsync(CancellationToken cancellationToken)
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

		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		await client.MappingBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Fail);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTestAddressDependentAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress1, It.IsAny<CancellationToken>())).ReturnsAsync(r3);

		await client.MappingBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.AddressDependent);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTestAddressAndPortDependentAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress1, It.IsAny<CancellationToken>())).ReturnsAsync(r3);

		await client.MappingBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.AddressAndPortDependent);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task MappingBehaviorTest3FailAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress1, It.IsAny<CancellationToken>())).ReturnsAsync(r3);

		await client.MappingBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Fail);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task FilteringBehaviorTestFailAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fail);

		await client.FilteringBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Fail);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNull();
		await Assert.That(client.State.LocalEndPoint).IsNull();
		await Assert.That(client.State.OtherEndPoint).IsNull();
	}

	[Test]
	public async Task FilteringBehaviorTestUnsupportedServerAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

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
			await client.FilteringBehaviorTestAsync(cancellationToken);

			await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
			await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
			await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.UnsupportedServer);
			await Assert.That(client.State.PublicEndPoint).IsNotNull();
			await Assert.That(client.State.LocalEndPoint).IsNotNull();
		}
	}

	[Test]
	public async Task FilteringBehaviorTestEndpointIndependentAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(r2);

		await client.FilteringBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.EndpointIndependent);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task FilteringBehaviorTest2UnsupportedServerAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(r2);

		await client.FilteringBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.UnsupportedServer);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task FilteringBehaviorTestAddressAndPortDependentAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.FilteringBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.AddressAndPortDependent);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task FilteringBehaviorTestAddressDependentAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(r3);

		await client.FilteringBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.AddressDependent);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task FilteringBehaviorTest3UnsupportedServerAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(r3);

		await client.FilteringBehaviorTestAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.UnsupportedServer);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task QueryFailTestAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!, true);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fail);

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Fail);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(client.State.PublicEndPoint).IsNull();
		await Assert.That(client.State.LocalEndPoint).IsNull();
		await Assert.That(client.State.OtherEndPoint).IsNull();
	}

	[Test]
	public async Task QueryUnsupportedServerTestAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.UnsupportedServer);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
	}

	[Test]
	public async Task QueryMappingBehaviorDirectTestAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.Direct);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.AddressAndPortDependent);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task QueryMappingBehaviorEndpointIndependentTestAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.EndpointIndependent);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.AddressAndPortDependent);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task QueryMappingBehaviorAddressAndPortDependentTestAsync(CancellationToken cancellationToken)
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
		mock.Setup(x => x.BindingTestBaseAsync(ServerAddress, It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress3, It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		mock.Setup(x => x.BindingTestBaseAsync(ChangedAddress1, It.IsAny<CancellationToken>())).ReturnsAsync(r3);
		mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(cancellationToken);

		await Assert.That(client.State.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(client.State.MappingBehavior).IsEqualTo(MappingBehavior.AddressAndPortDependent);
		await Assert.That(client.State.FilteringBehavior).IsEqualTo(FilteringBehavior.AddressAndPortDependent);
		await Assert.That(client.State.PublicEndPoint).IsNotNull();
		await Assert.That(client.State.LocalEndPoint).IsNotNull();
		await Assert.That(client.State.OtherEndPoint).IsNotNull();
	}
}
