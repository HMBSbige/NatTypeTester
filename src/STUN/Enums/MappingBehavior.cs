namespace STUN.Enums;

/// <summary>
/// Represents the NAT mapping behavior as defined in RFC 5780.
/// </summary>
public enum MappingBehavior
{
	/// <summary>
	/// The mapping behavior is unknown or has not been determined.
	/// </summary>
	Unknown,

	/// <summary>
	/// The STUN server does not support the required features for the mapping test.
	/// </summary>
	UnsupportedServer,

	/// <summary>
	/// No NAT is present; the client has a direct public address.
	/// </summary>
	Direct,

	/// <summary>
	/// Endpoint-Independent Mapping: the NAT reuses the same mapped address for all destinations.
	/// </summary>
	EndpointIndependent,

	/// <summary>
	/// Address-Dependent Mapping: the NAT assigns a different mapped address for each distinct destination address.
	/// </summary>
	AddressDependent,

	/// <summary>
	/// Address and Port-Dependent Mapping: the NAT assigns a different mapped address for each distinct destination address and port.
	/// </summary>
	AddressAndPortDependent,

	/// <summary>
	/// The mapping behavior test failed.
	/// </summary>
	Fail
}
