using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using STUN.Utils;
using System.Diagnostics;
using System.Net;

namespace STUN.Client;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc3489#section-10.1
/// </summary>
public class Stun3489NatTypeDiscovery(IPEndPoint server)
{
	public ClassicStunResult Result { get; } = new();

	private enum Phase
	{
		Test1,
		Test2,
		Test1_2,
		Test3,
		Done
	}

	private Phase _phase;
	private IPEndPoint? _changedAddress;
	private IPEndPoint? _mappedAddress1;
	private IPEndPoint? _test1Remote;

	public StunDiscoveryAction CreateQuery()
	{
		_phase = Phase.Test1;
		return CreateClassicBindingRequest(server);
	}

	public StunDiscoveryAction? GotResponse(StunResponse? response)
	{
		return _phase switch
		{
			Phase.Test1 => HandleTest1(response),
			Phase.Test2 => HandleTest2(response),
			Phase.Test1_2 => HandleTest1_2(response),
			Phase.Test3 => HandleTest3(response),
			_ => null
		};
	}

	private StunDiscoveryAction? HandleTest1(StunResponse? response)
	{
		if (response is null)
		{
			Result.NatType = NatType.UdpBlocked;
			_phase = Phase.Done;
			return null;
		}

		Result.LocalEndPoint = response.Local;
		_test1Remote = response.Remote;

		_mappedAddress1 = response.Message.GetMappedAddressAttribute();
		_changedAddress = response.Message.GetChangedAddressAttribute();

		Result.PublicEndPoint = _mappedAddress1;// 显示 test I 得到的映射地址

		// 某些单 IP 服务器的迷惑操作
		if (_mappedAddress1 is null || _changedAddress is null
									|| Equals(_changedAddress.Address, response.Remote.Address)
									|| _changedAddress.Port == response.Remote.Port)
		{
			Result.NatType = NatType.UnsupportedServer;
			_phase = Phase.Done;
			return null;
		}

		// Test II
		_phase = Phase.Test2;
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

	private StunDiscoveryAction? HandleTest2(StunResponse? response)
	{
		if (_test1Remote is null || _changedAddress is null)
		{
			throw new UnreachableException();
		}

		IPEndPoint? mappedAddress2 = response?.Message.GetMappedAddressAttribute();

		if (response is not null)
		{
			// 有些单 IP 服务器并不能测 NAT 类型
			if (Equals(_test1Remote.Address, response.Remote.Address) || _test1Remote.Port == response.Remote.Port)
			{
				Result.NatType = NatType.UnsupportedServer;
				Result.PublicEndPoint = mappedAddress2;
				_phase = Phase.Done;
				return null;
			}
		}

		// is Public IP == link's IP?
		if (Equals(_mappedAddress1, Result.LocalEndPoint))
		{
			// No NAT
			if (response is null)
			{
				Result.NatType = NatType.SymmetricUdpFirewall;
				Result.PublicEndPoint = _mappedAddress1;
			}
			else
			{
				Result.NatType = NatType.OpenInternet;
				Result.PublicEndPoint = mappedAddress2;
			}

			_phase = Phase.Done;
			return null;
		}

		// NAT
		if (response is not null)
		{
			Result.NatType = NatType.FullCone;
			Result.PublicEndPoint = mappedAddress2;
			_phase = Phase.Done;
			return null;
		}

		// Test I(#2) - send to changedAddress
		_phase = Phase.Test1_2;
		return CreateClassicBindingRequest(_changedAddress);
	}

	private StunDiscoveryAction? HandleTest1_2(StunResponse? response)
	{
		IPEndPoint? mappedAddress12 = response?.Message.GetMappedAddressAttribute();

		if (mappedAddress12 is null)
		{
			Result.NatType = NatType.Unknown;
			_phase = Phase.Done;
			return null;
		}

		if (!Equals(mappedAddress12, _mappedAddress1))
		{
			Result.NatType = NatType.Symmetric;
			Result.PublicEndPoint = mappedAddress12;
			_phase = Phase.Done;
			return null;
		}

		// Test III
		_phase = Phase.Test3;
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

	private StunDiscoveryAction? HandleTest3(StunResponse? response)
	{
		if (response is not null)
		{
			IPEndPoint? mappedAddress3 = response.Message.GetMappedAddressAttribute();

			if (mappedAddress3 is not null
				&& Equals(response.Remote.Address, _test1Remote?.Address)
				&& response.Remote.Port != _test1Remote.Port)
			{
				Result.NatType = NatType.RestrictedCone;
				Result.PublicEndPoint = mappedAddress3;
				_phase = Phase.Done;
				return null;
			}
		}

		Result.NatType = NatType.PortRestrictedCone;
		Result.PublicEndPoint = _mappedAddress1;
		_phase = Phase.Done;
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
