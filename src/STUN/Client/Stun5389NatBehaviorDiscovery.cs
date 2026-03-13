using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using STUN.Utils;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace STUN.Client;

/// <summary>
/// Implements the NAT behavior discovery algorithm as defined in RFC 5780 Section 4.2.
/// This is a pure state machine that produces <see cref="StunDiscoveryAction"/> steps
/// and consumes <see cref="StunResponse"/> results to determine NAT mapping and filtering behaviors.
/// </summary>
/// <param name="server">The STUN server endpoint to query.</param>
public class Stun5389NatBehaviorDiscovery(IPEndPoint server)
{
	/// <summary>
	/// Gets the RFC 5389/5780 STUN result containing the discovered NAT behaviors and endpoints.
	/// </summary>
	public StunResult5389 Result { get; } = new();

	private enum Scope
	{
		BindingOnly,
		Mapping,
		Filtering,
		Full
	}

	private enum Phase
	{
		BindingTest,
		FilteringTest2,
		FilteringTest3,
		MappingTest2,
		MappingTest3,
		Done
	}

	private Scope _scope;
	private Phase _phase;
	private IPEndPoint? _mappingTest2PublicEndPoint;

	/// <summary>
	/// Creates the initial query action to perform a full NAT behavior discovery (binding, filtering, and mapping tests).
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateQuery()
	{
		_scope = Scope.Full;
		_phase = Phase.BindingTest;
		return CreateBindingRequest(server);
	}

	/// <summary>
	/// Creates the initial query action to perform only the binding test.
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateBindingTest()
	{
		_scope = Scope.BindingOnly;
		_phase = Phase.BindingTest;
		return CreateBindingRequest(server);
	}

	/// <summary>
	/// Creates the initial query action to perform a NAT mapping behavior test.
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateMappingBehaviorTest()
	{
		_scope = Scope.Mapping;
		_phase = Phase.BindingTest;
		return CreateBindingRequest(server);
	}

	/// <summary>
	/// Creates the initial query action to perform a NAT filtering behavior test.
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateFilteringBehaviorTest()
	{
		_scope = Scope.Filtering;
		_phase = Phase.BindingTest;
		return CreateBindingRequest(server);
	}

	/// <summary>
	/// Processes a STUN response (or null for timeout) and returns the next action to perform.
	/// </summary>
	/// <param name="response">The STUN response received, or <see langword="null"/> if the request timed out.</param>
	/// <returns>The next discovery action to send, or <see langword="null"/> if the discovery is complete.</returns>
	public StunDiscoveryAction? GotResponse(StunResponse? response)
	{
		return _phase switch
		{
			Phase.BindingTest => HandleBindingTest(response),
			Phase.FilteringTest2 => HandleFilteringTest2(response),
			Phase.FilteringTest3 => HandleFilteringTest3(response),
			Phase.MappingTest2 => HandleMappingTest2(response),
			Phase.MappingTest3 => HandleMappingTest3(response),
			_ => null
		};
	}

	private StunDiscoveryAction? HandleBindingTest(StunResponse? response)
	{
		IPEndPoint? mappedAddress = response?.Message.GetXorMappedAddressAttribute();
		IPEndPoint? otherAddress = response?.Message.GetOtherAddressAttribute();

		if (response is null)
		{
			Result.BindingTestResult = BindingTestResult.Fail;
		}
		else if (mappedAddress is null)
		{
			Result.BindingTestResult = BindingTestResult.UnsupportedServer;
		}
		else
		{
			Result.BindingTestResult = BindingTestResult.Success;
		}

		Result.LocalEndPoint = response?.Local;
		Result.PublicEndPoint = mappedAddress;
		Result.OtherEndPoint = otherAddress;

		if (_scope is Scope.BindingOnly || Result.BindingTestResult is not BindingTestResult.Success)
		{
			goto end;
		}

		if (!HasValidOtherAddress(Result.OtherEndPoint))
		{
			if (_scope is Scope.Filtering or Scope.Full)
			{
				Result.FilteringBehavior = FilteringBehavior.UnsupportedServer;
			}

			if (_scope is Scope.Mapping)
			{
				Result.MappingBehavior = MappingBehavior.UnsupportedServer;
			}

			goto end;
		}

		if (_scope is Scope.Filtering or Scope.Full)
		{
			return TransitionToFilteringTest2();
		}

		if (_scope is Scope.Mapping)
		{
			return TransitionToMappingOrDone();
		}

	end:
		_phase = Phase.Done;
		return null;
	}

	private StunDiscoveryAction TransitionToFilteringTest2()
	{
		_phase = Phase.FilteringTest2;
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			Attributes = [AttributeExtensions.BuildChangeRequest(true, true)]
		};
		return new StunDiscoveryAction
		{
			Message = message,
			SendTo = server
		};
	}

	private StunDiscoveryAction? HandleFilteringTest2(StunResponse? response)
	{
		if (response is not null)
		{
			Result.FilteringBehavior = Equals(response.Remote, Result.OtherEndPoint)
				? FilteringBehavior.EndpointIndependent
				: FilteringBehavior.UnsupportedServer;
			return TransitionAfterFiltering();
		}

		// Test III
		_phase = Phase.FilteringTest3;
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			Attributes = [AttributeExtensions.BuildChangeRequest(false, true)]
		};
		return new StunDiscoveryAction
		{
			Message = message,
			SendTo = server
		};
	}

	private StunDiscoveryAction? HandleFilteringTest3(StunResponse? response)
	{
		if (response is null)
		{
			Result.FilteringBehavior = FilteringBehavior.AddressAndPortDependent;
		}
		else if (Equals(response.Remote.Address, server.Address) && response.Remote.Port != server.Port)
		{
			Result.FilteringBehavior = FilteringBehavior.AddressDependent;
		}
		else
		{
			Result.FilteringBehavior = FilteringBehavior.UnsupportedServer;
		}

		return TransitionAfterFiltering();
	}

	private StunDiscoveryAction? TransitionAfterFiltering()
	{
		if (_scope is Scope.Full && Result.FilteringBehavior is not FilteringBehavior.UnsupportedServer)
		{
			return TransitionToMappingOrDone();
		}

		_phase = Phase.Done;
		return null;
	}

	private StunDiscoveryAction? TransitionToMappingOrDone()
	{
		if (Equals(Result.PublicEndPoint, Result.LocalEndPoint))
		{
			Result.MappingBehavior = MappingBehavior.Direct;// or Endpoint-Independent
			_phase = Phase.Done;
			return null;
		}

		// Mapping test II: send to (otherIP, serverPort)
		_phase = Phase.MappingTest2;
		Debug.Assert(Result.OtherEndPoint is not null);
		IPEndPoint target = new(Result.OtherEndPoint.Address, server.Port);
		return CreateBindingRequest(target);
	}

	private StunDiscoveryAction? HandleMappingTest2(StunResponse? response)
	{
		IPEndPoint? mappedAddress = response?.Message.GetXorMappedAddressAttribute();

		if (mappedAddress is null)
		{
			Result.MappingBehavior = MappingBehavior.Fail;
			goto end;
		}

		if (Equals(mappedAddress, Result.PublicEndPoint))
		{
			Result.MappingBehavior = MappingBehavior.EndpointIndependent;
			goto end;
		}

		_mappingTest2PublicEndPoint = mappedAddress;

		// Mapping test III: send to otherEndPoint
		_phase = Phase.MappingTest3;
		return CreateBindingRequest(Result.OtherEndPoint!);

	end:
		_phase = Phase.Done;
		return null;
	}

	private StunDiscoveryAction? HandleMappingTest3(StunResponse? response)
	{
		IPEndPoint? mappedAddress = response?.Message.GetXorMappedAddressAttribute();

		if (mappedAddress is null)
		{
			Result.MappingBehavior = MappingBehavior.Fail;
		}
		else
		{
			Result.MappingBehavior = Equals(mappedAddress, _mappingTest2PublicEndPoint)
				? MappingBehavior.AddressDependent
				: MappingBehavior.AddressAndPortDependent;
		}

		_phase = Phase.Done;
		return null;
	}

	private static StunDiscoveryAction CreateBindingRequest(IPEndPoint sendTo)
	{
		StunMessage5389 message = new() { StunMessageType = StunMessageType.BindingRequest };
		return new StunDiscoveryAction
		{
			Message = message,
			SendTo = sendTo
		};
	}

	private bool HasValidOtherAddress([NotNullWhen(true)] IPEndPoint? other)
	{
		return other is not null && !Equals(other.Address, server.Address) && other.Port != server.Port;
	}
}
