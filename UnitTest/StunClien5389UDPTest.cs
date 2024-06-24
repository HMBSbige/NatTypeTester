using Dns.Net.Abstractions;
using Dns.Net.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using System.Net;

namespace UnitTest;

[TestClass]
public class StunClien5389UDPTest
{
	private readonly IDnsClient _dnsClient = new DefaultDnsClient();

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);
	private static readonly IPEndPoint LocalAddress1 = IPEndPoint.Parse(@"127.0.0.1:114");
	private static readonly IPEndPoint MappedAddress1 = IPEndPoint.Parse(@"1.1.1.1:114");
	private static readonly IPEndPoint MappedAddress2 = IPEndPoint.Parse(@"1.1.1.1:514");
	private static readonly IPEndPoint ServerAddress = IPEndPoint.Parse(@"2.2.2.2:1919");
	private static readonly IPEndPoint ChangedAddress1 = IPEndPoint.Parse(@"3.3.3.3:23333");
	private static readonly IPEndPoint ChangedAddress2 = IPEndPoint.Parse(@"2.2.2.2:810");
	private static readonly IPEndPoint ChangedAddress3 = IPEndPoint.Parse(@"3.3.3.3:1919");

	private static readonly StunMessage5389 DefaultStunMessage = new();

	[TestMethod]
	public async Task BindingTestSuccessAsync()
	{
		IPAddress ip = await _dnsClient.QueryAsync(@"stun.syncthing.net");
		using StunClient5389UDP client = new(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync();

		Assert.AreEqual(BindingTestResult.Success, response.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, response.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, response.FilteringBehavior);
		Assert.IsNotNull(response.PublicEndPoint);
		Assert.IsNotNull(response.LocalEndPoint);
		Assert.IsNotNull(response.OtherEndPoint);
	}

	[TestMethod]
	public async Task BindingTestFailAsync()
	{
		IPAddress ip = IPAddress.Parse(@"1.1.1.1");
		using StunClient5389UDP client = new(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync();

		Assert.AreEqual(BindingTestResult.Fail, response.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, response.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, response.FilteringBehavior);
		Assert.IsNull(response.PublicEndPoint);
		Assert.IsNull(response.LocalEndPoint);
		Assert.IsNull(response.OtherEndPoint);
	}

	[TestMethod]
	public async Task MappingBehaviorTestFailAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(fail);

		await client.MappingBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Fail, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNull(client.State.PublicEndPoint);
		Assert.IsNull(client.State.LocalEndPoint);
		Assert.IsNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task MappingBehaviorTestUnsupportedServerAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		async Task TestAsync()
		{
			await client.MappingBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.UnsupportedServer, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
		}
	}

	[TestMethod]
	public async Task MappingBehaviorTestDirectAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
		StunClient5389UDP client = mock.Object;

		StunResult5389 response = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = MappedAddress1,
			OtherEndPoint = ChangedAddress1
		};

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(response);

		await client.MappingBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Direct, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task MappingBehaviorTestEndpointIndependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ChangedAddress1
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		await client.MappingBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.EndpointIndependent, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task MappingBehaviorTest2FailAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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
			BindingTestResult = BindingTestResult.Fail,
		};

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ServerAddress, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress3, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);
		await client.MappingBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Fail, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task MappingBehaviorTestAddressDependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.MappingBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.AddressDependent, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task MappingBehaviorTestAddressAndPortDependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.MappingBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.AddressAndPortDependent, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task MappingBehaviorTest3FailAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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
			BindingTestResult = BindingTestResult.Fail
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ServerAddress, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress3, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r2);
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ChangedAddress1, ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r3);

		await client.MappingBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Fail, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task FilteringBehaviorTestFailAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(fail);

		await client.FilteringBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Fail, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNull(client.State.PublicEndPoint);
		Assert.IsNull(client.State.LocalEndPoint);
		Assert.IsNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task FilteringBehaviorTestUnsupportedServerAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		async Task TestAsync()
		{
			await client.FilteringBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.UnsupportedServer, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
		}
	}

	[TestMethod]
	public async Task FilteringBehaviorTestEndpointIndependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.FilteringBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.EndpointIndependent, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task FilteringBehaviorTest2UnsupportedServerAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.FilteringBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.UnsupportedServer, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task FilteringBehaviorTestAddressAndPortDependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.FilteringBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task FilteringBehaviorTestAddressDependentAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.FilteringBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.AddressDependent, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task FilteringBehaviorTest3UnsupportedServerAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.FilteringBehaviorTestAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.UnsupportedServer, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task QueryFailTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
		StunClient5389UDP client = mock.Object;

		StunResult5389 fail = new() { BindingTestResult = BindingTestResult.Fail };

		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(fail);

		await client.QueryAsync();

		Assert.AreEqual(BindingTestResult.Fail, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
		Assert.IsNull(client.State.PublicEndPoint);
		Assert.IsNull(client.State.LocalEndPoint);
		Assert.IsNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task QueryUnsupportedServerTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
		StunClient5389UDP client = mock.Object;

		StunResult5389 r1 = new()
		{
			BindingTestResult = BindingTestResult.Success,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = LocalAddress1,
			OtherEndPoint = ServerAddress
		};
		mock.Protected().Setup<ValueTask<StunResult5389>>(@"BindingTestBaseAsync", ItExpr.IsAny<IPEndPoint>(), ItExpr.IsAny<CancellationToken>()).ReturnsAsync(r1);

		await client.QueryAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.UnsupportedServer, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
	}

	[TestMethod]
	public async Task QueryMappingBehaviorDirectTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.QueryAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Direct, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task QueryMappingBehaviorEndpointIndependentTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.QueryAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.EndpointIndependent, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}

	[TestMethod]
	public async Task QueryMappingBehaviorAddressAndPortDependentTestAsync()
	{
		Mock<StunClient5389UDP> mock = new(ServerAddress, Any, default!);
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

		await client.QueryAsync();

		Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
		Assert.AreEqual(MappingBehavior.AddressAndPortDependent, client.State.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
		Assert.IsNotNull(client.State.PublicEndPoint);
		Assert.IsNotNull(client.State.LocalEndPoint);
		Assert.IsNotNull(client.State.OtherEndPoint);
	}
}
