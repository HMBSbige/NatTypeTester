using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using System.Diagnostics;
using System.Net;
using static STUN.Utils.AttributeExtensions;

namespace UnitTest;

/// <summary>
/// The tests that a <see cref="Stun5389NatBehaviorDiscovery"/> can run, each of which starts with the binding test.
/// </summary>
public enum DiscoveryScope
{
	BindingTest,
	MappingBehaviorTest,
	FilteringBehaviorTest,
	Query
}

public class Stun5389NatBehaviorDiscoveryTest
{
	private static readonly IPEndPoint LocalAddress1 = IPEndPoint.Parse(@"127.0.0.1:114");
	private static readonly IPEndPoint MappedAddress1 = IPEndPoint.Parse(@"1.1.1.1:114");
	private static readonly IPEndPoint MappedAddress2 = IPEndPoint.Parse(@"1.1.1.1:514");
	private static readonly IPEndPoint MappedAddress3 = IPEndPoint.Parse(@"1.1.1.1:810");
	private static readonly IPEndPoint ServerAddress = IPEndPoint.Parse(@"2.2.2.2:1919");
	private static readonly IPEndPoint ChangedAddress1 = IPEndPoint.Parse(@"3.3.3.3:23333");
	private static readonly IPEndPoint ChangedAddress2 = IPEndPoint.Parse(@"2.2.2.2:810");
	private static readonly IPEndPoint ChangedAddress3 = IPEndPoint.Parse(@"3.3.3.3:1919");

	/// <summary>
	/// The binding test response through a NAT from a server whose OTHER-ADDRESS is <see cref="ChangedAddress1"/>.
	/// </summary>
	private static StunResponse BindingResponse => Response(ServerAddress, MappedAddress1, ChangedAddress1);

	private static StunResult5389 BindingSucceeded => new()
	{
		BindingTestResult = BindingTestResult.Success,
		PublicEndPoint = MappedAddress1,
		OtherEndPoint = ChangedAddress1,
		LocalEndPoint = LocalAddress1
	};

	public static IEnumerable<Func<IPEndPoint?>> UnsupportedOtherAddresses()
	{
		yield return () => null;
		yield return () => new IPEndPoint(ChangedAddress2.Address, ChangedAddress2.Port);
		yield return () => new IPEndPoint(ChangedAddress3.Address, ChangedAddress3.Port);
	}

	public static IEnumerable<Func<(IPEndPoint?, FilteringBehavior)>> FilteringTest3Responses()
	{
		yield return () => (null, FilteringBehavior.AddressAndPortDependent);
		yield return () => (new IPEndPoint(ChangedAddress2.Address, ChangedAddress2.Port), FilteringBehavior.AddressDependent);
		yield return () => (new IPEndPoint(ServerAddress.Address, ServerAddress.Port), FilteringBehavior.UnsupportedServer);
	}

	private static Stun5389NatBehaviorDiscovery Start(DiscoveryScope scope)
	{
		Stun5389NatBehaviorDiscovery session = new(ServerAddress);
		_ = scope switch
		{
			DiscoveryScope.BindingTest => session.CreateBindingTest(),
			DiscoveryScope.MappingBehaviorTest => session.CreateMappingBehaviorTest(),
			DiscoveryScope.FilteringBehaviorTest => session.CreateFilteringBehaviorTest(),
			DiscoveryScope.Query => session.CreateQuery(),
			_ => throw new UnreachableException()
		};
		return session;
	}

	/// <summary>
	/// Creates a response from <paramref name="remote"/> to <paramref name="local"/>, or <see cref="LocalAddress1"/> by default,
	/// with MAPPED-ADDRESS <paramref name="mapped"/> and CHANGED-ADDRESS <paramref name="other"/> when they are given.
	/// </summary>
	private static StunResponse Response(IPEndPoint remote, IPEndPoint? mapped = null, IPEndPoint? other = null, IPEndPoint? local = null)
	{
		List<StunAttribute> attributes = [];

		if (mapped is not null)
		{
			attributes.Add(BuildMapping(IpFamily.IPv4, mapped.Address, (ushort)mapped.Port));
		}

		if (other is not null)
		{
			attributes.Add(BuildChangeAddress(IpFamily.IPv4, other.Address, (ushort)other.Port));
		}

		return new StunResponse(new StunMessage5389 { Attributes = attributes }, remote, local ?? LocalAddress1);
	}

	/// <summary>
	/// Passes <paramref name="response"/> to <paramref name="session"/>, which must ask for another request and update its result to <paramref name="expected"/>.
	/// </summary>
	private static async Task AssertContinuesAsync(Stun5389NatBehaviorDiscovery session, StunResponse? response, StunResult5389 expected)
	{
		await Assert.That(session.GotResponse(response)).IsNotNull();
		await Assert.That(session.Result).IsEqualTo(expected);
	}

	/// <summary>
	/// Passes <paramref name="response"/> to <paramref name="session"/>, which must end with the result <paramref name="expected"/>.
	/// </summary>
	private static async Task AssertEndsAsync(Stun5389NatBehaviorDiscovery session, StunResponse? response, StunResult5389 expected)
	{
		await Assert.That(session.GotResponse(response)).IsNull();
		await Assert.That(session.Result).IsEqualTo(expected);
	}

	#region BindingTest

	[Test]
	[MatrixDataSource]
	public async Task BindingTest_Fail([Matrix] DiscoveryScope scope)
	{
		await AssertEndsAsync(Start(scope), null, new StunResult5389
		{
			BindingTestResult = BindingTestResult.Fail
		});
	}

	[Test]
	[MatrixDataSource]
	public async Task BindingTest_UnsupportedServer([Matrix] DiscoveryScope scope)
	{
		// Binding response without XOR-MAPPED-ADDRESS
		await AssertEndsAsync(Start(scope), Response(ServerAddress), new StunResult5389
		{
			BindingTestResult = BindingTestResult.UnsupportedServer,
			LocalEndPoint = LocalAddress1
		});
	}

	[Test]
	public async Task BindingTest_Success()
	{
		await AssertEndsAsync(Start(DiscoveryScope.BindingTest), BindingResponse, BindingSucceeded);
	}

	#endregion

	#region MappingBehaviorTest

	[Test]
	[MethodDataSource(nameof(UnsupportedOtherAddresses))]
	public async Task MappingBehaviorTest_UnsupportedServer(IPEndPoint? otherAddress)
	{
		StunResponse response = Response(ServerAddress, MappedAddress1, otherAddress);

		await AssertEndsAsync(Start(DiscoveryScope.MappingBehaviorTest), response, BindingSucceeded with
		{
			MappingBehavior = MappingBehavior.UnsupportedServer,
			OtherEndPoint = otherAddress
		});
	}

	[Test]
	public async Task MappingBehaviorTest_Direct()
	{
		// Binding: public == local
		StunResponse response = Response(ServerAddress, MappedAddress1, ChangedAddress1, local: MappedAddress1);

		await AssertEndsAsync(Start(DiscoveryScope.MappingBehaviorTest), response, BindingSucceeded with
		{
			MappingBehavior = MappingBehavior.Direct,
			LocalEndPoint = MappedAddress1
		});
	}

	[Test]
	public async Task MappingBehaviorTest_EndpointIndependent()
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.MappingBehaviorTest);

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Test II - same mapped address
		await AssertEndsAsync(session, Response(ChangedAddress3, MappedAddress1), BindingSucceeded with { MappingBehavior = MappingBehavior.EndpointIndependent });
	}

	[Test]
	[Arguments(true, MappingBehavior.AddressDependent)]
	[Arguments(false, MappingBehavior.AddressAndPortDependent)]
	public async Task MappingBehaviorTest_Test3(bool sameMapping, MappingBehavior expected)
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.MappingBehaviorTest);

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Test II - different mapped address
		await AssertContinuesAsync(session, Response(ChangedAddress3, MappedAddress2), BindingSucceeded);
		// Test III compares its mapping with test II
		await AssertEndsAsync(session, Response(ChangedAddress1, sameMapping ? MappedAddress2 : MappedAddress3), BindingSucceeded with { MappingBehavior = expected });
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public async Task MappingBehaviorTest_Test2Fail(bool hasResponse)
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.MappingBehaviorTest);

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Test II receives no response or a response without a mapping attribute
		await AssertEndsAsync(session, hasResponse ? Response(ChangedAddress3) : null, BindingSucceeded with { MappingBehavior = MappingBehavior.Fail });
	}

	[Test]
	[Arguments(false)]
	[Arguments(true)]
	public async Task MappingBehaviorTest_Test3Fail(bool hasResponse)
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.MappingBehaviorTest);

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Test II - different mapped address
		await AssertContinuesAsync(session, Response(ChangedAddress3, MappedAddress2), BindingSucceeded);
		// Test III receives no response or a response without a mapping attribute
		await AssertEndsAsync(session, hasResponse ? Response(ChangedAddress1) : null, BindingSucceeded with { MappingBehavior = MappingBehavior.Fail });
	}

	#endregion

	#region FilteringBehaviorTest

	[Test]
	[MethodDataSource(nameof(UnsupportedOtherAddresses))]
	public async Task FilteringBehaviorTest_UnsupportedServer(IPEndPoint? otherAddress)
	{
		StunResponse response = Response(ServerAddress, MappedAddress1, otherAddress);

		await AssertEndsAsync(Start(DiscoveryScope.FilteringBehaviorTest), response, BindingSucceeded with
		{
			FilteringBehavior = FilteringBehavior.UnsupportedServer,
			OtherEndPoint = otherAddress
		});
	}

	[Test]
	[Arguments(true, FilteringBehavior.EndpointIndependent)]
	[Arguments(false, FilteringBehavior.UnsupportedServer)]
	public async Task FilteringBehaviorTest_Test2(bool responseFromOtherAddress, FilteringBehavior expected)
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.FilteringBehaviorTest);

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Test II requires a response from OtherAddress
		await AssertEndsAsync(session, Response(responseFromOtherAddress ? ChangedAddress1 : ServerAddress), BindingSucceeded with { FilteringBehavior = expected });
	}

	[Test]
	[MethodDataSource(nameof(FilteringTest3Responses))]
	public async Task FilteringBehaviorTest_Test3(IPEndPoint? respondingServer, FilteringBehavior expected)
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.FilteringBehaviorTest);

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Test II - no response
		await AssertContinuesAsync(session, null, BindingSucceeded);
		// Test III requires a response from the same IP and a different port
		await AssertEndsAsync(session, respondingServer is null ? null : Response(respondingServer), BindingSucceeded with { FilteringBehavior = expected });
	}

	#endregion

	#region Query (Filtering + Mapping combined)

	/// <summary>
	/// Walks through every request a query can send and checks its target and CHANGE-REQUEST flags against RFC 5780 §4.3 and §4.4.
	/// </summary>
	[Test]
	public async Task Query_RequestSequence()
	{
		Stun5389NatBehaviorDiscovery session = new(ServerAddress);

		await Assert.That(DiscoveryRequest.From(session.CreateQuery())).IsEqualTo(new DiscoveryRequest(ServerAddress));
		// Filtering test II: the primary address, changing both IP and port
		await Assert.That(DiscoveryRequest.From(session.GotResponse(BindingResponse))).IsEqualTo(new DiscoveryRequest(ServerAddress, ChangeIp: true, ChangePort: true));
		// Filtering test III, after test II gets no response: the primary address, changing only the port
		await Assert.That(DiscoveryRequest.From(session.GotResponse(null))).IsEqualTo(new DiscoveryRequest(ServerAddress, ChangePort: true));
		// Mapping test II, after filtering test III gets no response: the alternate IP and the primary port
		await Assert.That(DiscoveryRequest.From(session.GotResponse(null))).IsEqualTo(new DiscoveryRequest(new IPEndPoint(ChangedAddress1.Address, ServerAddress.Port)));
		// Mapping test III, after test II gets a different mapping: the alternate address and port
		await Assert.That(DiscoveryRequest.From(session.GotResponse(Response(ChangedAddress3, MappedAddress2)))).IsEqualTo(new DiscoveryRequest(ChangedAddress1));
	}

	[Test]
	public async Task Query_UnsupportedServer()
	{
		// Binding response without OTHER-ADDRESS
		await AssertEndsAsync(Start(DiscoveryScope.Query), Response(ServerAddress, MappedAddress1), BindingSucceeded with
		{
			FilteringBehavior = FilteringBehavior.UnsupportedServer,
			OtherEndPoint = null
		});
	}

	[Test]
	public async Task Query_Direct_FilteringAddressAndPortDependent()
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.Query);
		StunResult5389 direct = BindingSucceeded with { LocalEndPoint = MappedAddress1 };

		// Binding: public == local
		await AssertContinuesAsync(session, Response(ServerAddress, MappedAddress1, ChangedAddress1, local: MappedAddress1), direct);
		// Filtering tests II and III - no response
		await AssertContinuesAsync(session, null, direct);
		await AssertEndsAsync(session, null, direct with
		{
			MappingBehavior = MappingBehavior.Direct,
			FilteringBehavior = FilteringBehavior.AddressAndPortDependent
		});
	}

	[Test]
	public async Task Query_EndpointIndependent_FilteringAddressAndPortDependent()
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.Query);
		StunResult5389 filtered = BindingSucceeded with { FilteringBehavior = FilteringBehavior.AddressAndPortDependent };

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Filtering tests II and III - no response
		await AssertContinuesAsync(session, null, BindingSucceeded);
		await AssertContinuesAsync(session, null, filtered);
		// Mapping test II - same mapped address
		await AssertEndsAsync(session, Response(ChangedAddress3, MappedAddress1), filtered with { MappingBehavior = MappingBehavior.EndpointIndependent });
	}

	[Test]
	[Arguments(true, MappingBehavior.AddressAndPortDependent)]
	[Arguments(false, MappingBehavior.Fail)]
	public async Task Query_MappingTest3_FilteringAddressAndPortDependent(bool hasResponse, MappingBehavior expected)
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.Query);
		StunResult5389 filtered = BindingSucceeded with { FilteringBehavior = FilteringBehavior.AddressAndPortDependent };

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Filtering tests II and III - no response
		await AssertContinuesAsync(session, null, BindingSucceeded);
		await AssertContinuesAsync(session, null, filtered);
		// Mapping test II - different mapped address
		await AssertContinuesAsync(session, Response(ChangedAddress3, MappedAddress2), filtered);
		// Mapping test III receives a new mapping or no response
		await AssertEndsAsync(session, hasResponse ? Response(ChangedAddress1, MappedAddress3) : null, filtered with { MappingBehavior = expected });
	}

	[Test]
	public async Task Query_AddressDependent_FilteringEndpointIndependent()
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.Query);
		StunResult5389 filtered = BindingSucceeded with { FilteringBehavior = FilteringBehavior.EndpointIndependent };

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Filtering test II - response from OtherEndPoint
		await AssertContinuesAsync(session, Response(ChangedAddress1), filtered);
		// Mapping test II - different mapped address, which test III gets again
		await AssertContinuesAsync(session, Response(ChangedAddress3, MappedAddress2), filtered);
		await AssertEndsAsync(session, Response(ChangedAddress1, MappedAddress2), filtered with { MappingBehavior = MappingBehavior.AddressDependent });
	}

	[Test]
	public async Task Query_MappingTest2Fail_FilteringAddressDependent()
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.Query);
		StunResult5389 filtered = BindingSucceeded with { FilteringBehavior = FilteringBehavior.AddressDependent };

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Filtering test II - no response; test III - response from the same IP and a different port
		await AssertContinuesAsync(session, null, BindingSucceeded);
		await AssertContinuesAsync(session, Response(ChangedAddress2), filtered);
		// Mapping test II - no response
		await AssertEndsAsync(session, null, filtered with { MappingBehavior = MappingBehavior.Fail });
	}

	[Test]
	public async Task Query_FilteringTest2UnsupportedServer_SkipMapping()
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.Query);

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Filtering test II - response from wrong address
		await AssertEndsAsync(session, Response(ServerAddress), BindingSucceeded with { FilteringBehavior = FilteringBehavior.UnsupportedServer });
	}

	[Test]
	public async Task Query_FilteringTest3UnsupportedServer_SkipMapping()
	{
		Stun5389NatBehaviorDiscovery session = Start(DiscoveryScope.Query);

		await AssertContinuesAsync(session, BindingResponse, BindingSucceeded);
		// Filtering test II - no response; test III - response from same address (unsupported)
		await AssertContinuesAsync(session, null, BindingSucceeded);
		await AssertEndsAsync(session, Response(ServerAddress), BindingSucceeded with { FilteringBehavior = FilteringBehavior.UnsupportedServer });
	}

	#endregion
}
