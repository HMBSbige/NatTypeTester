using Dns.Net.Abstractions;
using Dns.Net.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.Utils;
using System.Net;
using System.Net.Sockets;
using static STUN.Utils.AttributeExtensions;

namespace UnitTest;

[TestClass]
public class StunClient3489Test
{
	private readonly IDnsClient _dnsClient = new DefaultDnsClient();

	private const string Server = @"stun.syncthing.net";
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

	[TestMethod]
	public async Task UdpBlockedTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!);
		StunClient3489 client = mock.Object;

		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync();
		Assert.AreEqual(NatType.UdpBlocked, client.State.NatType);
	}

	[TestMethod]
	public async Task UnsupportedServerTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!);
		StunClient3489 client = mock.Object;

		mock.Setup(x => x.LocalEndPoint).Returns(LocalAddress1);
		StunResponse unknownResponse = new(DefaultStunMessage, Any, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(unknownResponse);
		await TestAsync();

		StunResponse r1 = new(new StunMessage5389
		{
			Attributes = new[]
			{
				BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)
			}
		}, ServerAddress, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r1);
		await TestAsync();

		StunResponse r2 = new(new StunMessage5389
		{
			Attributes = new[]
			{
				BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
			}
		}, ServerAddress, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r2);
		await TestAsync();

		StunResponse r3 = new(new StunMessage5389
		{
			Attributes = new[]
			{
				BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
				BuildChangeAddress(IpFamily.IPv4, ServerAddress.Address, (ushort)ChangedAddress1.Port)
			}
		}, ServerAddress, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r3);
		await TestAsync();

		StunResponse r4 = new(new StunMessage5389
		{
			Attributes = new[]
			{
				BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
				BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ServerAddress.Port)
			}
		}, ServerAddress, LocalAddress1);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r4);
		await TestAsync();

		async Task TestAsync()
		{
			await client.QueryAsync();
			Assert.AreEqual(NatType.UnsupportedServer, client.State.NatType);
		}
	}

	[TestMethod]
	public async Task NoNatTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!);
		StunClient3489 client = mock.Object;

		StunResponse openInternetTest1Response = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
					BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
				}
			},
			ServerAddress,
			MappedAddress1
		);
		StunResponse test2Response = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)
				}
			},
			ChangedAddress1,
			MappedAddress1
		);

		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(openInternetTest1Response);
		mock.Setup(x => x.LocalEndPoint).Returns(MappedAddress1);
		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(test2Response);

		Assert.AreEqual(NatType.Unknown, client.State.NatType);
		await client.QueryAsync();
		Assert.AreEqual(NatType.OpenInternet, client.State.NatType);

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		await client.QueryAsync();
		Assert.AreEqual(NatType.SymmetricUdpFirewall, client.State.NatType);
	}

	[TestMethod]
	public async Task FullConeTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!);
		StunClient3489 client = mock.Object;

		StunResponse test1Response = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
					BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
				}
			},
			ServerAddress,
			LocalAddress1
		);
		StunResponse fullConeResponse = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)
				}
			},
			ChangedAddress1,
			LocalAddress1
		);
		StunResponse unsupportedResponse1 = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)
				}
			},
			ServerAddress,
			LocalAddress1
		);
		StunResponse unsupportedResponse2 = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)
				}
			},
			new IPEndPoint(ServerAddress.Address, ChangedAddress1.Port),
			LocalAddress1
		);
		StunResponse unsupportedResponse3 = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)
				}
			},
			new IPEndPoint(ChangedAddress1.Address, ServerAddress.Port),
			LocalAddress1
		);

		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(test1Response);
		mock.Setup(x => x.LocalEndPoint).Returns(LocalAddress1);
		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fullConeResponse);

		Assert.AreEqual(NatType.Unknown, client.State.NatType);
		await client.QueryAsync();
		Assert.AreEqual(NatType.FullCone, client.State.NatType);

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse1);
		await TestUnsupportedServerAsync();

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse2);
		await TestUnsupportedServerAsync();

		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(unsupportedResponse3);
		await TestUnsupportedServerAsync();

		async Task TestUnsupportedServerAsync()
		{
			await client.QueryAsync();
			Assert.AreEqual(NatType.UnsupportedServer, client.State.NatType);
		}
	}

	[TestMethod]
	public async Task SymmetricTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!);
		StunClient3489 client = mock.Object;

		StunResponse test1Response = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
					BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
				}
			},
			ServerAddress,
			LocalAddress1
		);
		StunResponse test12Response = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress2.Address, (ushort)MappedAddress2.Port),
					BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
				}
			},
			ServerAddress,
			LocalAddress1
		);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(test1Response);
		mock.Setup(x => x.LocalEndPoint).Returns(LocalAddress1);
		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.Test1_2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

		Assert.AreEqual(NatType.Unknown, client.State.NatType);
		await client.QueryAsync();
		Assert.AreEqual(NatType.Unknown, client.State.NatType);

		mock.Setup(x => x.Test1_2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(test12Response);

		await client.QueryAsync();
		Assert.AreEqual(NatType.Symmetric, client.State.NatType);
	}

	[TestMethod]
	public async Task RestrictedConeTestAsync()
	{
		Mock<StunClient3489> mock = new(Any, Any, default!);
		StunClient3489 client = mock.Object;

		StunResponse test1Response = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
					BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
				}
			},
			ServerAddress,
			LocalAddress1
		);
		StunResponse test3Response = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
					BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
				}
			},
			ChangedAddress2,
			LocalAddress1
		);
		StunResponse test3ErrorResponse = new(
			new StunMessage5389
			{
				Attributes = new[]
				{
					BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
					BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
				}
			},
			ServerAddress,
			LocalAddress1
		);
		mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(test1Response);
		mock.Setup(x => x.LocalEndPoint).Returns(LocalAddress1);
		mock.Setup(x => x.Test2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		mock.Setup(x => x.Test1_2Async(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(test1Response);

		mock.Setup(x => x.Test3Async(It.IsAny<CancellationToken>())).ReturnsAsync(test3Response);
		Assert.AreEqual(NatType.Unknown, client.State.NatType);
		await client.QueryAsync();
		Assert.AreEqual(NatType.RestrictedCone, client.State.NatType);

		mock.Setup(x => x.Test3Async(It.IsAny<CancellationToken>())).ReturnsAsync(test3ErrorResponse);
		await client.QueryAsync();
		Assert.AreEqual(NatType.PortRestrictedCone, client.State.NatType);

		mock.Setup(x => x.Test3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
		await client.QueryAsync();
		Assert.AreEqual(NatType.PortRestrictedCone, client.State.NatType);
	}

	[TestMethod]
	public async Task Test1Async()
	{
		IPAddress ip = await _dnsClient.QueryAsync(Server);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);

		// test I
		StunResponse? response1 = await client.Test1Async(default);

		Assert.IsNotNull(response1);
		Assert.AreEqual(ip, response1.Remote.Address);
		Assert.AreEqual(Port, response1.Remote.Port);
		Assert.AreNotEqual(Any, client.LocalEndPoint);

		IPEndPoint? mappedAddress = response1.Message.GetMappedAddressAttribute();
		IPEndPoint? changedAddress = response1.Message.GetChangedAddressAttribute();

		Assert.IsNotNull(mappedAddress);
		Assert.IsNotNull(changedAddress);

		Assert.AreNotEqual(ip, changedAddress.Address);
		Assert.AreNotEqual(Port, changedAddress.Port);

		// Test I(#2)
		StunResponse? response12 = await client.Test1_2Async(changedAddress, default);

		Assert.IsNotNull(response12);
		Assert.AreEqual(changedAddress.Address, response12.Remote.Address);
		Assert.AreEqual(changedAddress.Port, response12.Remote.Port);
	}

#if FullCone
	[TestMethod]
#endif
	public async Task Test2Async()
	{
		IPAddress ip = await _dnsClient.QueryAsync(Server);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);
		StunResponse? response2 = await client.Test2Async(ip.AddressFamily is AddressFamily.InterNetworkV6 ? IPv6Any : Any, default);

		Assert.IsNotNull(response2);

		Assert.AreNotEqual(ip, response2.Remote.Address);
		Assert.AreNotEqual(Port, response2.Remote.Port);
	}

#if FullCone
	[TestMethod]
#endif
	public async Task Test3Async()
	{
		IPAddress ip = await _dnsClient.QueryAsync(Server);
		using StunClient3489 client = new(new IPEndPoint(ip, Port), Any);
		StunResponse? response = await client.Test3Async(default);

		Assert.IsNotNull(response);

		Assert.AreEqual(ip, response.Remote.Address);
		Assert.AreNotEqual(Port, response.Remote.Port);
	}
}
