using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using STUN.Utils;
using System.Net;

namespace STUN.Client;

/// <summary>
/// Implements the classic NAT type discovery algorithm as defined in RFC 3489 Section 10.1.
/// This is a pure state machine that produces <see cref="StunDiscoveryAction"/> steps
/// and consumes <see cref="StunResponse"/> results to determine the NAT type.
/// </summary>
/// <param name="server">The STUN server endpoint to query.</param>
public class Stun3489NatTypeDiscovery(IPEndPoint server)
{
	/// <summary>
	/// Gets the classic STUN result containing the discovered NAT type and endpoints.
	/// </summary>
	public ClassicStunResult Result { get; } = new();

	private closed record Phase
	{
		public sealed record Test1 : Phase;

		public sealed record Test2(IPEndPoint Test1Remote, IPEndPoint MappedAddress1, IPEndPoint ChangedAddress) : Phase;

		public sealed record Test1_2(IPEndPoint Test1Remote, IPEndPoint MappedAddress1) : Phase;

		public sealed record Test3(IPEndPoint Test1Remote, IPEndPoint MappedAddress1) : Phase;

		public sealed record Done : Phase;
	}

	private Phase _phase = new Phase.Test1();

	/// <summary>
	/// Creates the initial query action to begin the RFC 3489 NAT type discovery process.
	/// </summary>
	/// <returns>The first STUN discovery action to send.</returns>
	public StunDiscoveryAction CreateQuery()
	{
		_phase = new Phase.Test1();
		return CreateClassicBindingRequest(server);
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
			Phase.Test1 => HandleTest1(response),
			Phase.Test2 phase => HandleTest2(phase, response),
			Phase.Test1_2 phase => HandleTest1_2(phase, response),
			Phase.Test3 phase => HandleTest3(phase, response),
			Phase.Done => null
		};
	}

	private StunDiscoveryAction? HandleTest1(StunResponse? response)
	{
		if (response is null)
		{
			Result.NatType = NatType.UdpBlocked;
			_phase = new Phase.Done();
			return null;
		}

		Result.LocalEndPoint = response.Local;

		IPEndPoint? mappedAddress1 = response.Message.GetMappedAddressAttribute();
		IPEndPoint? changedAddress = response.Message.GetChangedAddressAttribute();

		Result.PublicEndPoint = mappedAddress1;// 显示 test I 得到的映射地址

		// 某些单 IP 服务器的迷惑操作
		if (mappedAddress1 is null || changedAddress is null
									|| Equals(changedAddress.Address, response.Remote.Address)
									|| changedAddress.Port == response.Remote.Port)
		{
			Result.NatType = NatType.UnsupportedServer;
			_phase = new Phase.Done();
			return null;
		}

		// Test II
		_phase = new Phase.Test2(response.Remote, mappedAddress1, changedAddress);
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			MagicCookie = 0,
			Attributes = [AttributeExtensions.BuildChangeRequest(true, true)]
		};
		return new StunDiscoveryAction
		{
			Message = message,
			SendTo = server
		};
	}

	private StunDiscoveryAction? HandleTest2(Phase.Test2 phase, StunResponse? response)
	{
		IPEndPoint? mappedAddress2 = response?.Message.GetMappedAddressAttribute();

		if (response is not null)
		{
			// 有些单 IP 服务器并不能测 NAT 类型
			if (Equals(phase.Test1Remote.Address, response.Remote.Address) || phase.Test1Remote.Port == response.Remote.Port)
			{
				Result.NatType = NatType.UnsupportedServer;
				Result.PublicEndPoint = mappedAddress2;
				_phase = new Phase.Done();
				return null;
			}
		}

		// is Public IP == link's IP?
		if (Equals(phase.MappedAddress1, Result.LocalEndPoint))
		{
			// No NAT
			if (response is null)
			{
				Result.NatType = NatType.SymmetricUdpFirewall;
				Result.PublicEndPoint = phase.MappedAddress1;
			}
			else
			{
				Result.NatType = NatType.OpenInternet;
				Result.PublicEndPoint = mappedAddress2;
			}

			_phase = new Phase.Done();
			return null;
		}

		// NAT
		if (response is not null)
		{
			Result.NatType = NatType.FullCone;
			Result.PublicEndPoint = mappedAddress2;
			_phase = new Phase.Done();
			return null;
		}

		// Test I(#2) - send to changedAddress
		_phase = new Phase.Test1_2(phase.Test1Remote, phase.MappedAddress1);
		return CreateClassicBindingRequest(phase.ChangedAddress);
	}

	private StunDiscoveryAction? HandleTest1_2(Phase.Test1_2 phase, StunResponse? response)
	{
		IPEndPoint? mappedAddress12 = response?.Message.GetMappedAddressAttribute();

		if (mappedAddress12 is null)
		{
			Result.NatType = NatType.Unknown;
			_phase = new Phase.Done();
			return null;
		}

		if (!Equals(mappedAddress12, phase.MappedAddress1))
		{
			Result.NatType = NatType.Symmetric;
			Result.PublicEndPoint = mappedAddress12;
			_phase = new Phase.Done();
			return null;
		}

		// Test III
		_phase = new Phase.Test3(phase.Test1Remote, phase.MappedAddress1);
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			MagicCookie = 0,
			Attributes = [AttributeExtensions.BuildChangeRequest(false, true)]
		};
		return new StunDiscoveryAction
		{
			Message = message,
			SendTo = server
		};
	}

	private StunDiscoveryAction? HandleTest3(Phase.Test3 phase, StunResponse? response)
	{
		if (response is not null)
		{
			IPEndPoint? mappedAddress3 = response.Message.GetMappedAddressAttribute();

			if (mappedAddress3 is not null
				&& Equals(response.Remote.Address, phase.Test1Remote.Address)
				&& response.Remote.Port != phase.Test1Remote.Port)
			{
				Result.NatType = NatType.RestrictedCone;
				Result.PublicEndPoint = mappedAddress3;
				_phase = new Phase.Done();
				return null;
			}
		}

		Result.NatType = NatType.PortRestrictedCone;
		Result.PublicEndPoint = phase.MappedAddress1;
		_phase = new Phase.Done();
		return null;
	}

	private static StunDiscoveryAction CreateClassicBindingRequest(IPEndPoint sendTo)
	{
		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			MagicCookie = 0
		};
		return new StunDiscoveryAction
		{
			Message = message,
			SendTo = sendTo
		};
	}
}
