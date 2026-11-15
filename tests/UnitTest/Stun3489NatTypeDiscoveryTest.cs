using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using System.Net;
using static STUN.Utils.AttributeExtensions;

namespace UnitTest;

public class Stun3489NatTypeDiscoveryTest
{
	private static readonly IPEndPoint LocalAddress1 = IPEndPoint.Parse(@"127.0.0.1:114");
	private static readonly IPEndPoint MappedAddress1 = IPEndPoint.Parse(@"1.1.1.1:114");
	private static readonly IPEndPoint MappedAddress2 = IPEndPoint.Parse(@"1.1.1.1:514");
	private static readonly IPEndPoint ServerAddress = IPEndPoint.Parse(@"2.2.2.2:1919");
	private static readonly IPEndPoint ChangedAddress1 = IPEndPoint.Parse(@"3.3.3.3:23333");
	private static readonly IPEndPoint ChangedAddress2 = IPEndPoint.Parse(@"2.2.2.2:810");

	private static ClassicStunResult Test1Succeeded => new()
	{
		PublicEndPoint = MappedAddress1,
		LocalEndPoint = LocalAddress1
	};

	private static StunResponse CreateTest1Response(IPEndPoint mapped, IPEndPoint changed, IPEndPoint remote, IPEndPoint local)
	{
		return new StunResponse
		(
			new StunMessage5389
			{
				Attributes =
				[
					BuildMapping(IpFamily.IPv4, mapped.Address, (ushort)mapped.Port),
					BuildChangeAddress(IpFamily.IPv4, changed.Address, (ushort)changed.Port)
				]
			},
			remote,
			local
		);
	}

	private static StunResponse CreateMappedResponse(IPEndPoint mapped, IPEndPoint remote, IPEndPoint local)
	{
		return new StunResponse
		(
			new StunMessage5389
			{
				Attributes =
				[
					BuildMapping(IpFamily.IPv4, mapped.Address, (ushort)mapped.Port)
				]
			},
			remote,
			local
		);
	}

	[Test]
	public async Task UdpBlocked()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		await Assert.That(session.CreateQuery()).IsNotNull();

		await Assert.That(session.GotResponse(null)).IsNull();
		await Assert.That(session.Result).IsEqualTo(new ClassicStunResult
		{
			NatType = NatType.UdpBlocked
		});
	}

	[Test]
	[Arguments(false, false, DisplayName = "UnsupportedServer_NoAttributes")]
	[Arguments(true, false, DisplayName = "UnsupportedServer_NoChangedAddress")]
	[Arguments(false, true, DisplayName = "UnsupportedServer_NoMappedAddress")]
	public async Task UnsupportedServer_MissingAttributes(bool includeMappedAddress, bool includeChangedAddress)
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		List<StunAttribute> attributes = [];

		if (includeMappedAddress)
		{
			attributes.Add(BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port));
		}

		if (includeChangedAddress)
		{
			attributes.Add(BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port));
		}

		StunResponse response = new(new StunMessage5389 { Attributes = attributes }, ServerAddress, LocalAddress1);
		await Assert.That(session.GotResponse(response)).IsNull();
		await Assert.That(session.Result).IsEqualTo(new ClassicStunResult
		{
			NatType = NatType.UnsupportedServer,
			PublicEndPoint = includeMappedAddress ? MappedAddress1 : null,
			LocalEndPoint = LocalAddress1
		});
	}

	[Test]
	[Arguments(true, false, DisplayName = "UnsupportedServer_ChangedAddressSameIP")]
	[Arguments(false, true, DisplayName = "UnsupportedServer_ChangedAddressSamePort")]
	public async Task UnsupportedServer_InvalidChangedAddress(bool sameIp, bool samePort)
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		IPEndPoint changed = new(sameIp ? ServerAddress.Address : ChangedAddress1.Address, samePort ? ServerAddress.Port : ChangedAddress1.Port);
		StunResponse response = CreateTest1Response(MappedAddress1, changed, ServerAddress, LocalAddress1);
		await Assert.That(session.GotResponse(response)).IsNull();
		await Assert.That(session.Result).IsEqualTo(Test1Succeeded with { NatType = NatType.UnsupportedServer });
	}

	[Test]
	[Arguments(true, true, DisplayName = "UnsupportedServer_Test2ResponseFromSameAddress")]
	[Arguments(true, false, DisplayName = "UnsupportedServer_Test2ResponseFromSameIPOnly")]
	[Arguments(false, true, DisplayName = "UnsupportedServer_Test2ResponseFromSamePort")]
	public async Task UnsupportedServer_Test2InvalidRemote(bool sameIp, bool samePort)
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		await Assert.That(session.GotResponse(r1)).IsNotNull();

		// Test II: the server must change both IP and port
		IPEndPoint remote = new(sameIp ? ServerAddress.Address : ChangedAddress1.Address, samePort ? ServerAddress.Port : ChangedAddress1.Port);
		StunResponse r2 = CreateMappedResponse(MappedAddress1, remote, LocalAddress1);
		await Assert.That(session.GotResponse(r2)).IsNull();
		await Assert.That(session.Result).IsEqualTo(Test1Succeeded with { NatType = NatType.UnsupportedServer });
	}

	[Test]
	[Arguments(true, NatType.OpenInternet, DisplayName = "OpenInternet")]
	[Arguments(false, NatType.SymmetricUdpFirewall, DisplayName = "SymmetricUdpFirewall")]
	public async Task NoNat(bool receiveResponse, NatType expected)
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I: mapped == local
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, MappedAddress1);
		await Assert.That(session.GotResponse(r1)).IsNotNull();

		// Test II
		StunResponse? r2 = receiveResponse ? CreateMappedResponse(MappedAddress1, ChangedAddress1, MappedAddress1) : null;
		await Assert.That(session.GotResponse(r2)).IsNull();
		await Assert.That(session.Result).IsEqualTo(new ClassicStunResult
		{
			NatType = expected,
			PublicEndPoint = MappedAddress1,
			LocalEndPoint = MappedAddress1
		});
	}

	[Test]
	public async Task FullCone()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I: mapped != local (NAT detected)
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		await Assert.That(session.GotResponse(r1)).IsNotNull();

		// Test II: response received from changed address
		StunResponse r2 = CreateMappedResponse(MappedAddress1, ChangedAddress1, LocalAddress1);
		await Assert.That(session.GotResponse(r2)).IsNull();
		await Assert.That(session.Result).IsEqualTo(Test1Succeeded with { NatType = NatType.FullCone });
	}

	[Test]
	[Arguments(true, NatType.Symmetric, DisplayName = "Symmetric")]
	[Arguments(false, NatType.Unknown, DisplayName = "Unknown_Test12Fails")]
	public async Task Test12Completion(bool receiveResponse, NatType expected)
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		await Assert.That(session.GotResponse(r1)).IsNotNull();

		// Test II: no response
		await Assert.That(session.GotResponse(null)).IsNotNull();

		// Test I(#2): different mapped address or no response
		StunResponse? r12 = receiveResponse ? CreateMappedResponse(MappedAddress2, ChangedAddress1, LocalAddress1) : null;
		await Assert.That(session.GotResponse(r12)).IsNull();
		await Assert.That(session.Result).IsEqualTo(Test1Succeeded with { NatType = expected, PublicEndPoint = receiveResponse ? MappedAddress2 : MappedAddress1 });
	}

	[Test]
	[Arguments(true, true, NatType.RestrictedCone, DisplayName = "RestrictedCone")]
	[Arguments(false, false, NatType.PortRestrictedCone, DisplayName = "PortRestrictedCone_Test3Null")]
	[Arguments(true, false, NatType.PortRestrictedCone, DisplayName = "PortRestrictedCone_Test3WrongRemote")]
	public async Task Test3Completion(bool receiveResponse, bool changedPort, NatType expected)
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		await Assert.That(session.GotResponse(r1)).IsNotNull();

		// Test II: no response
		await Assert.That(session.GotResponse(null)).IsNotNull();

		// Test I(#2): same mapped address
		StunResponse r12 = CreateMappedResponse(MappedAddress1, ChangedAddress1, LocalAddress1);
		await Assert.That(session.GotResponse(r12)).IsNotNull();

		// Test III: response must come from the same IP and a different port
		StunResponse? r3 = receiveResponse ? CreateMappedResponse(MappedAddress1, changedPort ? ChangedAddress2 : ServerAddress, LocalAddress1) : null;
		await Assert.That(session.GotResponse(r3)).IsNull();
		await Assert.That(session.Result).IsEqualTo(Test1Succeeded with { NatType = expected });
	}
}
