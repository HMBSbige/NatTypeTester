using Dns.Net.Clients;
using Moq;
using Shouldly;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using System.Net;
using System.Net.Sockets;
using static STUN.Utils.AttributeExtensions;

namespace UnitTest;

public class StunClient3489Test : TestBase
{
	private readonly DefaultDnsClient _dnsClient = new();

	private const string Server = @"stun.hot-chilli.net";
	private const ushort Port = 3478;

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);
	private static readonly IPEndPoint IPv6Any = new(IPAddress.IPv6Any, 0);
	private static readonly IPEndPoint LocalAddress1 = IPEndPoint.Parse(@"127.0.0.1:114");
	private static readonly IPEndPoint MappedAddress1 = IPEndPoint.Parse(@"1.1.1.1:114");
	private static readonly IPEndPoint MappedAddress2 = IPEndPoint.Parse(@"1.1.1.1:514");
	private static readonly IPEndPoint ServerAddress = IPEndPoint.Parse(@"2.2.2.2:1919");
	private static readonly IPEndPoint ChangedAddress1 = IPEndPoint.Parse(@"3.3.3.3:23333");
	private static readonly IPEndPoint ChangedAddress2 = IPEndPoint.Parse(@"2.2.2.2:810");

	private static readonly StunMessage5389 DefaultStunMessage = new();

	[Fact]
	public async Task UdpBlockedTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!, true);
		StunClient3489 client = mock.Object;

		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.UdpBlocked);
	}

	[Fact]
	public async Task UnsupportedServerTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!, true);
		StunClient3489 client = mock.Object;

		mock.Setup(x => x.LocalEndPoint).Returns(LocalAddress1);
		StunResponse unknownResponse = new(DefaultStunMessage, Any, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(unknownResponse);
		await TestAsync();

		StunResponse r1 = new(new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)] }, ServerAddress, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		await TestAsync();

		StunResponse r2 = new(new StunMessage5389 { Attributes = [BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] }, ServerAddress, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		await TestAsync();

		StunResponse r3 = new(new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port), BuildChangeAddress(IpFamily.IPv4, ServerAddress.Address, (ushort)ChangedAddress1.Port)] }, ServerAddress, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r3);
		await TestAsync();

		StunResponse r4 = new(new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port), BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ServerAddress.Port)] }, ServerAddress, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r4);
		await TestAsync();

		return;

		async Task TestAsync()
		{
			await client.QueryAsync(CancellationToken);
			client.State.NatType.ShouldBe(NatType.UnsupportedServer);
		}
	}

	[Fact]
	public async Task NoNatTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!, true);
		StunClient3489 client = mock.Object;

		StunResponse openInternetTest1Response = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port), BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] },
			ServerAddress,
			MappedAddress1
		);
		StunResponse test2Response = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)] },
			ChangedAddress1,
			MappedAddress1
		);

		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(openInternetTest1Response);
		mock.Setup(x => x.LocalEndPoint).Returns(MappedAddress1);
		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(test2Response);

		client.State.NatType.ShouldBe(NatType.Unknown);
		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.OpenInternet);

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.SymmetricUdpFirewall);
	}

	[Fact]
	public async Task FullConeTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!, true);
		StunClient3489 client = mock.Object;

		StunResponse test1Response = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port), BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] },
			ServerAddress,
			LocalAddress1
		);
		StunResponse fullConeResponse = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)] },
			ChangedAddress1,
			LocalAddress1
		);
		StunResponse unsupportedResponse1 = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)] },
			ServerAddress,
			LocalAddress1
		);
		StunResponse unsupportedResponse2 = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)] },
			new IPEndPoint(ServerAddress.Address, ChangedAddress1.Port),
			LocalAddress1
		);
		StunResponse unsupportedResponse3 = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)] },
			new IPEndPoint(ChangedAddress1.Address, ServerAddress.Port),
			LocalAddress1
		);

		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(test1Response);
		mock.Setup(x => x.LocalEndPoint).Returns(LocalAddress1);
		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fullConeResponse);

		client.State.NatType.ShouldBe(NatType.Unknown);
		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.FullCone);

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse1);
		await TestUnsupportedServerAsync();

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse2);
		await TestUnsupportedServerAsync();

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse3);
		await TestUnsupportedServerAsync();

		return;

		async Task TestUnsupportedServerAsync()
		{
			await client.QueryAsync(CancellationToken);
			client.State.NatType.ShouldBe(NatType.UnsupportedServer);
		}
	}

	[Fact]
	public async Task SymmetricTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!, true);
		StunClient3489 client = mock.Object;

		StunResponse test1Response = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port), BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] },
			ServerAddress,
			LocalAddress1
		);
		StunResponse test12Response = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress2.Address, (ushort)MappedAddress2.Port), BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] },
			ServerAddress,
			LocalAddress1
		);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(test1Response);
		mock.Setup(x => x.LocalEndPoint).Returns(LocalAddress1);
		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.Test1_2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		client.State.NatType.ShouldBe(NatType.Unknown);
		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.Unknown);

		mock.Setup(x => x.Test1_2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(test12Response);

		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.Symmetric);
	}

	[Fact]
	public async Task RestrictedConeTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!, true);
		StunClient3489 client = mock.Object;

		StunResponse test1Response = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port), BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] },
			ServerAddress,
			LocalAddress1
		);
		StunResponse test3Response = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port), BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] },
			ChangedAddress2,
			LocalAddress1
		);
		StunResponse test3ErrorResponse = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port), BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] },
			ServerAddress,
			LocalAddress1
		);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(test1Response);
		mock.Setup(x => x.LocalEndPoint).Returns(LocalAddress1);
		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.Test1_2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(test1Response);

		mock.Setup(x => x.Test3Async(It.IsAny<CancellationToken>())).ReturnsAsync(test3Response);
		client.State.NatType.ShouldBe(NatType.Unknown);
		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.RestrictedCone);

		mock.Setup(x => x.Test3Async(It.IsAny<CancellationToken>())).ReturnsAsync(test3ErrorResponse);
		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.PortRestrictedCone);

		mock.Setup(x => x.Test3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		await client.QueryAsync(CancellationToken);
		client.State.NatType.ShouldBe(NatType.PortRestrictedCone);
	}

	[Fact]
	public async Task Test1Async()
	{
		IPAddress ip = await _dnsClient.QueryAsync(Server, CancellationToken);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);

		// test I
		StunResponse? response1 = await client.Test1Async(CancellationToken);

		response1.ShouldNotBeNull();
		response1.Remote.Address.ShouldBe(ip);
		response1.Remote.Port.ShouldBe(Port);
		client.LocalEndPoint.ShouldNotBe(Any);

		IPEndPoint? mappedAddress = response1.Message.GetMappedAddressAttribute();
		IPEndPoint? changedAddress = response1.Message.GetChangedAddressAttribute();

		mappedAddress.ShouldNotBeNull();
		changedAddress.ShouldNotBeNull();

		changedAddress.Address.ShouldNotBe(ip);
		changedAddress.Port.ShouldNotBe(Port);

		// Test I(#2)
		StunResponse? response12 = await client.Test1_2Async(changedAddress, CancellationToken);

		response12.ShouldNotBeNull();
		response12.Remote.Address.ShouldBe(changedAddress.Address);
		response12.Remote.Port.ShouldBe(changedAddress.Port);
	}

	[Fact(Skip = "FullCone", SkipUnless = nameof(TestEnvironment.IsFullCone), SkipType = typeof(TestEnvironment))]
	public async Task Test2Async()
	{
		IPAddress ip = await _dnsClient.QueryAsync(Server, CancellationToken);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);
		StunResponse? response2 = await client.Test2Async(ip.AddressFamily is AddressFamily.InterNetworkV6 ? IPv6Any : Any, CancellationToken);

		response2.ShouldNotBeNull();

		response2.Remote.Address.ShouldBe(ip);
		response2.Remote.Port.ShouldBe(Port);
	}

	[Fact(Skip = "FullCone", SkipUnless = nameof(TestEnvironment.IsFullCone), SkipType = typeof(TestEnvironment))]
	public async Task Test3Async()
	{
		IPAddress ip = await _dnsClient.QueryAsync(Server, CancellationToken);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);
		StunResponse? response = await client.Test3Async(CancellationToken);

		response.ShouldNotBeNull();
		response.Remote.Address.ShouldBe(ip);
		response.Remote.Port.ShouldNotBe(Port);
	}
}
