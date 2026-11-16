using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using STUN.Utils;
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

	private closed record Phase
	{
		public sealed record BindingTest : Phase;

		public sealed record FilteringTest2(IPEndPoint OtherEndPoint) : Phase;

		public sealed record FilteringTest3(IPEndPoint OtherEndPoint) : Phase;

		public sealed record MappingTest2(IPEndPoint OtherEndPoint) : Phase;

		public sealed record MappingTest3(IPEndPoint MappingTest2PublicEndPoint) : Phase;

		public sealed record Done : Phase;
	}

	private Scope _scope;
	private Phase _phase = new Phase.BindingTest();

	/// <summary>
	/// Creates the initial query action to perform a full NAT behavior discovery (binding, filtering, and mapping tests).
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateQuery()
	{
		_scope = Scope.Full;
		_phase = new Phase.BindingTest();
		return CreateBindingRequest(server);
	}

	/// <summary>
	/// Creates the initial query action to perform only the binding test.
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateBindingTest()
	{
		_scope = Scope.BindingOnly;
		_phase = new Phase.BindingTest();
		return CreateBindingRequest(server);
	}

	/// <summary>
	/// Creates the initial query action to perform a NAT mapping behavior test.
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateMappingBehaviorTest()
	{
		_scope = Scope.Mapping;
		_phase = new Phase.BindingTest();
		return CreateBindingRequest(server);
	}

	/// <summary>
	/// Creates the initial query action to perform a NAT filtering behavior test.
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateFilteringBehaviorTest()
	{
		_scope = Scope.Filtering;
		_phase = new Phase.BindingTest();
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
			Phase.FilteringTest2 phase => HandleFilteringTest2(phase.OtherEndPoint, response),
			Phase.FilteringTest3 phase => HandleFilteringTest3(phase.OtherEndPoint, response),
			Phase.MappingTest2 phase => HandleMappingTest2(phase.OtherEndPoint, response),
			Phase.MappingTest3 phase => HandleMappingTest3(phase.MappingTest2PublicEndPoint, response),
			Phase.Done => null
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
			_phase = new Phase.Done();
			return null;
		}

		if (!HasValidOtherAddress(otherAddress))
		{
			if (_scope is Scope.Filtering or Scope.Full)
			{
				Result.FilteringBehavior = FilteringBehavior.UnsupportedServer;
			}

			if (_scope is Scope.Mapping)
			{
				Result.MappingBehavior = MappingBehavior.UnsupportedServer;
			}

			_phase = new Phase.Done();
			return null;
		}

		if (_scope is Scope.Filtering or Scope.Full)
		{
			return TransitionToFilteringTest2(otherAddress);
		}

		return TransitionToMappingOrDone(otherAddress);
	}

	private StunDiscoveryAction TransitionToFilteringTest2(IPEndPoint otherEndPoint)
	{
		_phase = new Phase.FilteringTest2(otherEndPoint);
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

	private StunDiscoveryAction? HandleFilteringTest2(IPEndPoint otherEndPoint, StunResponse? response)
	{
		if (response is not null)
		{
			Result.FilteringBehavior = Equals(response.Remote, otherEndPoint)
				? FilteringBehavior.EndpointIndependent
				: FilteringBehavior.UnsupportedServer;
			return TransitionAfterFiltering(otherEndPoint);
		}

		// Test III
		_phase = new Phase.FilteringTest3(otherEndPoint);
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

	private StunDiscoveryAction? HandleFilteringTest3(IPEndPoint otherEndPoint, StunResponse? response)
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

		return TransitionAfterFiltering(otherEndPoint);
	}

	private StunDiscoveryAction? TransitionAfterFiltering(IPEndPoint otherEndPoint)
	{
		if (_scope is Scope.Full && Result.FilteringBehavior is not FilteringBehavior.UnsupportedServer)
		{
			return TransitionToMappingOrDone(otherEndPoint);
		}

		_phase = new Phase.Done();
		return null;
	}

	private StunDiscoveryAction? TransitionToMappingOrDone(IPEndPoint otherEndPoint)
	{
		if (Equals(Result.PublicEndPoint, Result.LocalEndPoint))
		{
			Result.MappingBehavior = MappingBehavior.Direct;// or Endpoint-Independent
			_phase = new Phase.Done();
			return null;
		}

		// Mapping test II: send to (otherIP, serverPort)
		_phase = new Phase.MappingTest2(otherEndPoint);
		IPEndPoint target = new(otherEndPoint.Address, server.Port);
		return CreateBindingRequest(target);
	}

	private StunDiscoveryAction? HandleMappingTest2(IPEndPoint otherEndPoint, StunResponse? response)
	{
		IPEndPoint? mappedAddress = response?.Message.GetXorMappedAddressAttribute();

		if (mappedAddress is null)
		{
			Result.MappingBehavior = MappingBehavior.Fail;
		}
		else if (Equals(mappedAddress, Result.PublicEndPoint))
		{
			Result.MappingBehavior = MappingBehavior.EndpointIndependent;
		}
		else
		{
			// Mapping test III: send to otherEndPoint
			_phase = new Phase.MappingTest3(mappedAddress);
			return CreateBindingRequest(otherEndPoint);
		}

		_phase = new Phase.Done();
		return null;
	}

	private StunDiscoveryAction? HandleMappingTest3(IPEndPoint mappingTest2PublicEndPoint, StunResponse? response)
	{
		IPEndPoint? mappedAddress = response?.Message.GetXorMappedAddressAttribute();

		if (mappedAddress is null)
		{
			Result.MappingBehavior = MappingBehavior.Fail;
		}
		else
		{
			Result.MappingBehavior = Equals(mappedAddress, mappingTest2PublicEndPoint)
				? MappingBehavior.AddressDependent
				: MappingBehavior.AddressAndPortDependent;
		}

		_phase = new Phase.Done();
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
