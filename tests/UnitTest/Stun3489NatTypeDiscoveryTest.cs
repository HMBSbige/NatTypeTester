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

	/// <summary>
	/// The test I response through a NAT from a server whose CHANGED-ADDRESS is <see cref="ChangedAddress1"/>.
	/// </summary>
	private static StunResponse Test1Response => Response(ServerAddress, MappedAddress1, ChangedAddress1);

	private static ClassicStunResult Test1Succeeded => new()
	{
		PublicEndPoint = MappedAddress1,
		LocalEndPoint = LocalAddress1
	};

	/// <summary>
	/// Creates a response from <paramref name="remote"/> to <paramref name="local"/>, or <see cref="LocalAddress1"/> by default,
	/// with MAPPED-ADDRESS <paramref name="mapped"/> and CHANGED-ADDRESS <paramref name="changed"/> when they are given.
	/// </summary>
	private static StunResponse Response(IPEndPoint remote, IPEndPoint? mapped = null, IPEndPoint? changed = null, IPEndPoint? local = null)
	{
		List<StunAttribute> attributes = [];

		if (mapped is not null)
		{
			attributes.Add(BuildMapping(IpFamily.IPv4, mapped.Address, (ushort)mapped.Port));
		}

		if (changed is not null)
		{
			attributes.Add(BuildChangeAddress(IpFamily.IPv4, changed.Address, (ushort)changed.Port));
		}

		return new StunResponse(new StunMessage5389 { Attributes = attributes }, remote, local ?? LocalAddress1);
	}

	[Test]
	public async Task UdpBlocked()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

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

		IPEndPoint? mapped = includeMappedAddress ? MappedAddress1 : null;
		StunResponse response = Response(ServerAddress, mapped, includeChangedAddress ? ChangedAddress1 : null);
		await Assert.That(session.GotResponse(response)).IsNull();
		await Assert.That(session.Result).IsEqualTo(new ClassicStunResult
		{
			NatType = NatType.UnsupportedServer,
			PublicEndPoint = mapped,
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
		await Assert.That(session.GotResponse(Response(ServerAddress, MappedAddress1, changed))).IsNull();
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

		await Assert.That(session.GotResponse(Test1Response)).IsNotNull();

		// Test II: the server must change both IP and port
		IPEndPoint remote = new(sameIp ? ServerAddress.Address : ChangedAddress1.Address, samePort ? ServerAddress.Port : ChangedAddress1.Port);
		await Assert.That(session.GotResponse(Response(remote, MappedAddress1))).IsNull();
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
		await Assert.That(session.GotResponse(Response(ServerAddress, MappedAddress1, ChangedAddress1, local: MappedAddress1))).IsNotNull();

		StunResponse? r2 = receiveResponse ? Response(ChangedAddress1, MappedAddress1, local: MappedAddress1) : null;
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
		await Assert.That(session.GotResponse(Test1Response)).IsNotNull();

		// Test II: response received from changed address
		await Assert.That(session.GotResponse(Response(ChangedAddress1, MappedAddress1))).IsNull();
		await Assert.That(session.Result).IsEqualTo(Test1Succeeded with { NatType = NatType.FullCone });
	}

	[Test]
	[Arguments(true, NatType.Symmetric, DisplayName = "Symmetric")]
	[Arguments(false, NatType.Unknown, DisplayName = "Unknown_Test12Fails")]
	public async Task Test12Completion(bool receiveResponse, NatType expected)
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		await Assert.That(session.GotResponse(Test1Response)).IsNotNull();

		// Test II: no response
		await Assert.That(session.GotResponse(null)).IsNotNull();

		// Test I(#2): different mapped address or no response
		StunResponse? r12 = receiveResponse ? Response(ChangedAddress1, MappedAddress2) : null;
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

		await Assert.That(session.GotResponse(Test1Response)).IsNotNull();

		// Test II: no response
		await Assert.That(session.GotResponse(null)).IsNotNull();

		// Test I(#2): same mapped address
		await Assert.That(session.GotResponse(Response(ChangedAddress1, MappedAddress1))).IsNotNull();

		// Test III: response must come from the same IP and a different port
		StunResponse? r3 = receiveResponse ? Response(changedPort ? ChangedAddress2 : ServerAddress, MappedAddress1) : null;
		await Assert.That(session.GotResponse(r3)).IsNull();
		await Assert.That(session.Result).IsEqualTo(Test1Succeeded with { NatType = expected });
	}

	/// <summary>
	/// Walks through every request the discovery can send and checks its target and CHANGE-REQUEST flags against RFC 3489 §10.1.
	/// </summary>
	[Test]
	public async Task RequestSequence()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);

		// Test I: no flags
		await Assert.That(DiscoveryRequest.From(session.CreateQuery())).IsEqualTo(new DiscoveryRequest(ServerAddress));
		// Test II: changing both IP and port
		await Assert.That(DiscoveryRequest.From(session.GotResponse(Test1Response))).IsEqualTo(new DiscoveryRequest(ServerAddress, ChangeIp: true, ChangePort: true));
		// Test I again, after test II gets no response: to CHANGED-ADDRESS
		await Assert.That(DiscoveryRequest.From(session.GotResponse(null))).IsEqualTo(new DiscoveryRequest(ChangedAddress1));
		// Test III, after test I gets the same mapping again: changing only the port
		await Assert.That(DiscoveryRequest.From(session.GotResponse(Response(ChangedAddress1, MappedAddress1)))).IsEqualTo(new DiscoveryRequest(ServerAddress, ChangePort: true));
	}
}
