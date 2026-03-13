using STUN.Client;
using STUN.Enums;
using STUN.Messages;
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

	[Before(Class)]
	public static async Task VerifyTestAddresses(ClassHookContext context)
	{
		using (Assert.Multiple())
		{
			// NAT 场景需要 mapped != local
			await Assert.That(MappedAddress1).IsNotEqualTo(LocalAddress1);
			// Symmetric 需要两个不同的 mapped
			await Assert.That(MappedAddress2).IsNotEqualTo(MappedAddress1);
			// ChangedAddress1 必须与 Server 的 IP 和端口都不同（有效的 CHANGED-ADDRESS）
			await Assert.That(ChangedAddress1.Address).IsNotEqualTo(ServerAddress.Address);
			await Assert.That(ChangedAddress1.Port).IsNotEqualTo(ServerAddress.Port);
			// ChangedAddress2 用于 Test III：同 IP 不同端口
			await Assert.That(ChangedAddress2.Address).IsEqualTo(ServerAddress.Address);
			await Assert.That(ChangedAddress2.Port).IsNotEqualTo(ServerAddress.Port);
		}
	}

	private static StunResponse CreateTest1Response(IPEndPoint mapped, IPEndPoint changed, IPEndPoint remote, IPEndPoint local)
	{
		return new StunResponse(
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
		return new StunResponse(
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
		StunDiscoveryAction? action = session.CreateQuery();
		await Assert.That(action).IsNotNull();

		action = session.GotResponse(null);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UdpBlocked);
	}

	[Test]
	public async Task UnsupportedServer_NoAttributes()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		StunResponse response = new(new StunMessage5389(), ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(response);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UnsupportedServer);
	}

	[Test]
	public async Task UnsupportedServer_NoChangedAddress()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		StunResponse response = new(
			new StunMessage5389 { Attributes = [BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)] },
			ServerAddress,
			LocalAddress1
		);
		StunDiscoveryAction? action = session.GotResponse(response);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UnsupportedServer);
	}

	[Test]
	public async Task UnsupportedServer_NoMappedAddress()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		StunResponse response = new(
			new StunMessage5389 { Attributes = [BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)] },
			ServerAddress,
			LocalAddress1
		);
		StunDiscoveryAction? action = session.GotResponse(response);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UnsupportedServer);
	}

	[Test]
	public async Task UnsupportedServer_ChangedAddressSameIP()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		StunResponse response = CreateTest1Response(MappedAddress1, new IPEndPoint(ServerAddress.Address, ChangedAddress1.Port), ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(response);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UnsupportedServer);
	}

	[Test]
	public async Task UnsupportedServer_ChangedAddressSamePort()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		StunResponse response = CreateTest1Response(MappedAddress1, new IPEndPoint(ChangedAddress1.Address, ServerAddress.Port), ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(response);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UnsupportedServer);
	}

	[Test]
	public async Task UnsupportedServer_Test2ResponseFromSameAddress()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II - response from same address as test I (unsupported)
		StunResponse r2 = CreateMappedResponse(MappedAddress1, ServerAddress, LocalAddress1);
		action = session.GotResponse(r2);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UnsupportedServer);
	}

	[Test]
	public async Task UnsupportedServer_Test2ResponseFromSameIPOnly()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II - response from same IP but different port
		StunResponse r2 = CreateMappedResponse(MappedAddress1, new IPEndPoint(ServerAddress.Address, ChangedAddress1.Port), LocalAddress1);
		action = session.GotResponse(r2);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UnsupportedServer);
	}

	[Test]
	public async Task UnsupportedServer_Test2ResponseFromSamePort()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II - response from same port (different IP)
		StunResponse r2 = CreateMappedResponse(MappedAddress1, new IPEndPoint(ChangedAddress1.Address, ServerAddress.Port), LocalAddress1);
		action = session.GotResponse(r2);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.UnsupportedServer);
	}

	[Test]
	public async Task OpenInternet()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I: mapped == local
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, MappedAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II: response received
		StunResponse r2 = CreateMappedResponse(MappedAddress1, ChangedAddress1, MappedAddress1);
		action = session.GotResponse(r2);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.OpenInternet);
	}

	[Test]
	public async Task SymmetricUdpFirewall()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I: mapped == local
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, MappedAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II: no response
		action = session.GotResponse(null);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.SymmetricUdpFirewall);
	}

	[Test]
	public async Task FullCone()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I: mapped != local (NAT detected)
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II: response received from changed address
		StunResponse r2 = CreateMappedResponse(MappedAddress1, ChangedAddress1, LocalAddress1);
		action = session.GotResponse(r2);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.FullCone);
	}

	[Test]
	public async Task Symmetric()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II: no response
		action = session.GotResponse(null);
		await Assert.That(action).IsNotNull();

		// Test I(#2): different mapped address
		StunResponse r12 = CreateMappedResponse(MappedAddress2, ChangedAddress1, LocalAddress1);
		action = session.GotResponse(r12);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.Symmetric);
	}

	[Test]
	public async Task Unknown_Test12Fails()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II: no response
		action = session.GotResponse(null);
		await Assert.That(action).IsNotNull();

		// Test I(#2): no response (null mapped)
		action = session.GotResponse(null);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.Unknown);
	}

	[Test]
	public async Task RestrictedCone()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II: no response
		action = session.GotResponse(null);
		await Assert.That(action).IsNotNull();

		// Test I(#2): same mapped address
		StunResponse r12 = CreateMappedResponse(MappedAddress1, ChangedAddress1, LocalAddress1);
		action = session.GotResponse(r12);
		await Assert.That(action).IsNotNull();

		// Test III: response from same IP, different port
		StunResponse r3 = CreateMappedResponse(MappedAddress1, ChangedAddress2, LocalAddress1);
		action = session.GotResponse(r3);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.RestrictedCone);
	}

	[Test]
	public async Task PortRestrictedCone_Test3Null()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II: no response
		action = session.GotResponse(null);
		await Assert.That(action).IsNotNull();

		// Test I(#2): same mapped address
		StunResponse r12 = CreateMappedResponse(MappedAddress1, ChangedAddress1, LocalAddress1);
		action = session.GotResponse(r12);
		await Assert.That(action).IsNotNull();

		// Test III: no response
		action = session.GotResponse(null);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.PortRestrictedCone);
	}

	[Test]
	public async Task PortRestrictedCone_Test3WrongRemote()
	{
		Stun3489NatTypeDiscovery session = new(ServerAddress);
		_ = session.CreateQuery();

		// Test I
		StunResponse r1 = CreateTest1Response(MappedAddress1, ChangedAddress1, ServerAddress, LocalAddress1);
		StunDiscoveryAction? action = session.GotResponse(r1);
		await Assert.That(action).IsNotNull();

		// Test II: no response
		action = session.GotResponse(null);
		await Assert.That(action).IsNotNull();

		// Test I(#2): same mapped address
		StunResponse r12 = CreateMappedResponse(MappedAddress1, ChangedAddress1, LocalAddress1);
		action = session.GotResponse(r12);
		await Assert.That(action).IsNotNull();

		// Test III: response from same address (not changed port)
		StunResponse r3 = CreateMappedResponse(MappedAddress1, ServerAddress, LocalAddress1);
		action = session.GotResponse(r3);
		await Assert.That(action).IsNull();
		await Assert.That(session.Result.NatType).IsEqualTo(NatType.PortRestrictedCone);
	}
}
