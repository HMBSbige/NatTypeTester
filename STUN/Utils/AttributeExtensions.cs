using STUN.Enums;
using STUN.Messages;
using STUN.Messages.StunAttributeValues;
using System;
using System.Linq;
using System.Net;

namespace STUN.Utils
{
	public static class AttributeExtensions
	{
		public static StunAttribute BuildChangeRequest(bool changeIp, bool changePort)
		{
			return new StunAttribute
			{
				Type = AttributeType.ChangeRequest,
				Length = 4,
				Value = new ChangeRequestStunAttributeValue { ChangeIp = changeIp, ChangePort = changePort }
			};
		}

		public static StunAttribute BuildMapping(IpFamily family, IPAddress ip, ushort port)
		{
			var length = family switch
			{
				IpFamily.IPv4 => 4,
				IpFamily.IPv6 => 16,
				_ => throw new ArgumentOutOfRangeException(nameof(family), family, null)
			};
			return new StunAttribute
			{
				Type = AttributeType.MappedAddress,
				Length = (ushort)(4 + length),
				Value = new MappedAddressStunAttributeValue
				{
					Family = family,
					Address = ip,
					Port = port
				}
			};
		}

		public static StunAttribute BuildChangeAddress(IpFamily family, IPAddress ip, ushort port)
		{
			var length = family switch
			{
				IpFamily.IPv4 => 4,
				IpFamily.IPv6 => 16,
				_ => throw new ArgumentOutOfRangeException(nameof(family), family, null)
			};
			return new StunAttribute
			{
				Type = AttributeType.ChangedAddress,
				Length = (ushort)(4 + length),
				Value = new ChangedAddressStunAttributeValue
				{
					Family = family,
					Address = ip,
					Port = port
				}
			};
		}

		public static IPEndPoint? GetMappedAddressAttribute(this StunMessage5389 response)
		{
			var mappedAddressAttribute = response.Attributes.FirstOrDefault(t => t.Type == AttributeType.MappedAddress);

			if (mappedAddressAttribute is null)
			{
				return null;
			}

			var mapped = (MappedAddressStunAttributeValue)mappedAddressAttribute.Value;
			return new IPEndPoint(mapped.Address!, mapped.Port);
		}

		public static IPEndPoint? GetChangedAddressAttribute(this StunMessage5389 response)
		{
			var changedAddressAttribute = response.Attributes.FirstOrDefault(t => t.Type == AttributeType.ChangedAddress);

			if (changedAddressAttribute is null)
			{
				return null;
			}

			var address = (ChangedAddressStunAttributeValue)changedAddressAttribute.Value;
			return new IPEndPoint(address.Address!, address.Port);
		}

		public static IPEndPoint? GetXorMappedAddressAttribute(this StunMessage5389 response)
		{
			var mappedAddressAttribute =
				response.Attributes.FirstOrDefault(t => t.Type == AttributeType.XorMappedAddress) ??
				response.Attributes.FirstOrDefault(t => t.Type == AttributeType.MappedAddress);

			if (mappedAddressAttribute is null)
			{
				return null;
			}

			var mapped = (AddressStunAttributeValue)mappedAddressAttribute.Value;
			return new IPEndPoint(mapped.Address!, mapped.Port);
		}

		public static IPEndPoint? GetOtherAddressAttribute(this StunMessage5389 response)
		{
			var addressAttribute =
				response.Attributes.FirstOrDefault(t => t.Type == AttributeType.OtherAddress) ??
				response.Attributes.FirstOrDefault(t => t.Type == AttributeType.ChangedAddress);

			if (addressAttribute is null)
			{
				return null;
			}

			var address = (AddressStunAttributeValue)addressAttribute.Value;
			return new IPEndPoint(address.Address!, address.Port);
		}
	}
}
