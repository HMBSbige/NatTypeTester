using Dns.Net.Clients;
using Moq;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using System.Net;
using System.Net.Sockets;
using static STUN.Utils.AttributeExtensions;

namespace UnitTest;

public class StunClient3489Test
{
	private readonly DefaultAClient _dnsClient = new();

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

	[Test]
	public async Task UdpBlockedTestAsync(CancellationToken cancellationToken)
	{
		Mock<StunClient3489> mock = new(Any, Any, default!, true);
		StunClient3489 client = mock.Object;

		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.UdpBlocked);
	}

	[Test]
	public async Task UnsupportedServerTestAsync(CancellationToken cancellationToken)
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
			await client.QueryAsync(cancellationToken);
			await Assert.That(client.State.NatType).IsEqualTo(NatType.UnsupportedServer);
		}
	}

	[Test]
	public async Task NoNatTestAsync(CancellationToken cancellationToken)
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

		await Assert.That(client.State.NatType).IsEqualTo(NatType.Unknown);
		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.OpenInternet);

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.SymmetricUdpFirewall);
	}

	[Test]
	public async Task FullConeTestAsync(CancellationToken cancellationToken)
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

		await Assert.That(client.State.NatType).IsEqualTo(NatType.Unknown);
		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.FullCone);

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse1);
		await TestUnsupportedServerAsync();

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse2);
		await TestUnsupportedServerAsync();

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse3);
		await TestUnsupportedServerAsync();

		return;

		async Task TestUnsupportedServerAsync()
		{
			await client.QueryAsync(cancellationToken);
			await Assert.That(client.State.NatType).IsEqualTo(NatType.UnsupportedServer);
		}
	}

	[Test]
	public async Task SymmetricTestAsync(CancellationToken cancellationToken)
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

		await Assert.That(client.State.NatType).IsEqualTo(NatType.Unknown);
		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.Unknown);

		mock.Setup(x => x.Test1_2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(test12Response);

		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.Symmetric);
	}

	[Test]
	public async Task RestrictedConeTestAsync(CancellationToken cancellationToken)
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
		await Assert.That(client.State.NatType).IsEqualTo(NatType.Unknown);
		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.RestrictedCone);

		mock.Setup(x => x.Test3Async(It.IsAny<CancellationToken>())).ReturnsAsync(test3ErrorResponse);
		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.PortRestrictedCone);

		mock.Setup(x => x.Test3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		await client.QueryAsync(cancellationToken);
		await Assert.That(client.State.NatType).IsEqualTo(NatType.PortRestrictedCone);
	}

	[Test]
	public async Task Test1Async(CancellationToken cancellationToken)
	{
		IPAddress ip = await _dnsClient.QueryAsync(Server, cancellationToken);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);

		// test I
		StunResponse? response1 = await client.Test1Async(cancellationToken);

		await Assert.That(response1).IsNotNull();
		await Assert.That(response1.Remote.Address).IsEqualTo(ip);
		await Assert.That(response1.Remote.Port).IsEqualTo(Port);
		await Assert.That(client.LocalEndPoint).IsNotEqualTo(Any);

		IPEndPoint? mappedAddress = response1.Message.GetMappedAddressAttribute();
		IPEndPoint? changedAddress = response1.Message.GetChangedAddressAttribute();

		await Assert.That(mappedAddress).IsNotNull();
		await Assert.That(changedAddress).IsNotNull();

		await Assert.That(changedAddress.Address).IsNotEqualTo(ip);
		await Assert.That(changedAddress.Port).IsNotEqualTo(Port);

		// Test I(#2)
		StunResponse? response12 = await client.Test1_2Async(changedAddress, cancellationToken);

		await Assert.That(response12).IsNotNull();
		await Assert.That(response12.Remote.Address).IsEqualTo(changedAddress.Address);
		await Assert.That(response12.Remote.Port).IsEqualTo(changedAddress.Port);
	}

	[Test]
	public async Task Test2Async(CancellationToken cancellationToken)
	{
		Skip.Unless(TestEnvironment.IsFullCone, "FullCone");
		IPAddress ip = await _dnsClient.QueryAsync(Server, cancellationToken);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);
		StunResponse? response2 = await client.Test2Async(ip.AddressFamily is AddressFamily.InterNetworkV6 ? IPv6Any : Any, cancellationToken);

		await Assert.That(response2).IsNotNull();

		await Assert.That(response2.Remote.Address).IsEqualTo(ip);
		await Assert.That(response2.Remote.Port).IsEqualTo(Port);
	}

	[Test]
	public async Task Test3Async(CancellationToken cancellationToken)
	{
		Skip.Unless(TestEnvironment.IsFullCone, "FullCone");
		IPAddress ip = await _dnsClient.QueryAsync(Server, cancellationToken);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);
		StunResponse? response = await client.Test3Async(cancellationToken);

		await Assert.That(response).IsNotNull();
		await Assert.That(response.Remote.Address).IsEqualTo(ip);
		await Assert.That(response.Remote.Port).IsNotEqualTo(Port);
	}
}
