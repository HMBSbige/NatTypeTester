using STUN.Enums;
using STUN.Messages;
using STUN.Messages.StunAttributeValues;
using System.Net;

namespace STUN.Utils;

/// <summary>
/// Provides factory methods for building STUN attributes and extension methods for extracting address attributes from STUN responses.
/// </summary>
public static class AttributeExtensions
{
	/// <summary>
	/// Builds a CHANGE-REQUEST attribute used to request that the server send the response from a different IP and/or port.
	/// </summary>
	/// <param name="changeIp">Whether to request the server to use a different IP address.</param>
	/// <param name="changePort">Whether to request the server to use a different port.</param>
	/// <returns>A <see cref="StunAttribute"/> representing the CHANGE-REQUEST attribute.</returns>
	public static StunAttribute BuildChangeRequest(bool changeIp, bool changePort)
	{
		return new StunAttribute
		{
			Type = AttributeType.ChangeRequest,
			Length = 4,
			Value = new ChangeRequestStunAttributeValue { ChangeIp = changeIp, ChangePort = changePort }
		};
	}

	/// <summary>
	/// Builds a MAPPED-ADDRESS attribute containing the specified address and port.
	/// </summary>
	/// <param name="family">The IP address family (IPv4 or IPv6).</param>
	/// <param name="ip">The IP address to include in the attribute.</param>
	/// <param name="port">The port number to include in the attribute.</param>
	/// <returns>A <see cref="StunAttribute"/> representing the MAPPED-ADDRESS attribute.</returns>
	public static StunAttribute BuildMapping(IpFamily family, IPAddress ip, ushort port)
	{
		return new StunAttribute
		{
			Type = AttributeType.MappedAddress,
			Length = (ushort)(4 + GetAddressLength(family)),
			Value = new MappedAddressStunAttributeValue
			{
				Family = family,
				Address = ip,
				Port = port
			}
		};
	}

	/// <summary>
	/// Builds a CHANGED-ADDRESS attribute containing the alternate server address and port.
	/// </summary>
	/// <param name="family">The IP address family (IPv4 or IPv6).</param>
	/// <param name="ip">The IP address to include in the attribute.</param>
	/// <param name="port">The port number to include in the attribute.</param>
	/// <returns>A <see cref="StunAttribute"/> representing the CHANGED-ADDRESS attribute.</returns>
	public static StunAttribute BuildChangeAddress(IpFamily family, IPAddress ip, ushort port)
	{
		return new StunAttribute
		{
			Type = AttributeType.ChangedAddress,
			Length = (ushort)(4 + GetAddressLength(family)),
			Value = new ChangedAddressStunAttributeValue
			{
				Family = family,
				Address = ip,
				Port = port
			}
		};
	}

	private static int GetAddressLength(IpFamily family)
	{
		return family switch
		{
			IpFamily.IPv4 => 4,
			IpFamily.IPv6 => 16,
			_ => throw new ArgumentOutOfRangeException(nameof(family), family, null)
		};
	}

	/// <summary>
	/// Extracts the MAPPED-ADDRESS attribute from a STUN response as an <see cref="IPEndPoint"/>.
	/// </summary>
	/// <param name="response">The STUN response message to extract from.</param>
	/// <returns>The mapped address endpoint, or <see langword="null"/> if the attribute is not present.</returns>
	public static IPEndPoint? GetMappedAddressAttribute(this StunMessage5389 response)
	{
		StunAttribute? mappedAddressAttribute = response.Attributes.FirstOrDefault(t => t.Type == AttributeType.MappedAddress);

		if (mappedAddressAttribute is null)
		{
			return null;
		}

		MappedAddressStunAttributeValue mapped = (MappedAddressStunAttributeValue)mappedAddressAttribute.Value;
		return ToEndPoint(mapped);
	}

	/// <summary>
	/// Extracts the CHANGED-ADDRESS attribute from a STUN response as an <see cref="IPEndPoint"/>.
	/// </summary>
	/// <param name="response">The STUN response message to extract from.</param>
	/// <returns>The changed address endpoint, or <see langword="null"/> if the attribute is not present.</returns>
	public static IPEndPoint? GetChangedAddressAttribute(this StunMessage5389 response)
	{
		StunAttribute? changedAddressAttribute = response.Attributes.FirstOrDefault(t => t.Type == AttributeType.ChangedAddress);

		if (changedAddressAttribute is null)
		{
			return null;
		}

		ChangedAddressStunAttributeValue address = (ChangedAddressStunAttributeValue)changedAddressAttribute.Value;
		return ToEndPoint(address);
	}

	/// <summary>
	/// Extracts the XOR-MAPPED-ADDRESS attribute from a STUN response as an <see cref="IPEndPoint"/>.
	/// Falls back to MAPPED-ADDRESS if XOR-MAPPED-ADDRESS is not present.
	/// </summary>
	/// <param name="response">The STUN response message to extract from.</param>
	/// <returns>The XOR-mapped or mapped address endpoint, or <see langword="null"/> if neither attribute is present.</returns>
	public static IPEndPoint? GetXorMappedAddressAttribute(this StunMessage5389 response)
	{
		StunAttribute? mappedAddressAttribute =
			response.Attributes.FirstOrDefault(t => t.Type == AttributeType.XorMappedAddress) ??
			response.Attributes.FirstOrDefault(t => t.Type == AttributeType.MappedAddress);

		if (mappedAddressAttribute is null)
		{
			return null;
		}

		AddressStunAttributeValue mapped = (AddressStunAttributeValue)mappedAddressAttribute.Value;
		return ToEndPoint(mapped);
	}

	/// <summary>
	/// Extracts the OTHER-ADDRESS attribute from a STUN response as an <see cref="IPEndPoint"/>.
	/// Falls back to CHANGED-ADDRESS if OTHER-ADDRESS is not present.
	/// </summary>
	/// <param name="response">The STUN response message to extract from.</param>
	/// <returns>The other address endpoint, or <see langword="null"/> if neither attribute is present.</returns>
	public static IPEndPoint? GetOtherAddressAttribute(this StunMessage5389 response)
	{
		StunAttribute? addressAttribute =
			response.Attributes.FirstOrDefault(t => t.Type == AttributeType.OtherAddress) ??
			response.Attributes.FirstOrDefault(t => t.Type == AttributeType.ChangedAddress);

		if (addressAttribute is null)
		{
			return null;
		}

		AddressStunAttributeValue address = (AddressStunAttributeValue)addressAttribute.Value;
		return ToEndPoint(address);
	}

	private static IPEndPoint ToEndPoint(AddressStunAttributeValue value)
	{
		return value.Address is { } address
			? new IPEndPoint(address, value.Port)
			: throw new InvalidOperationException(@"STUN address attribute is missing IP address.");
	}
}
