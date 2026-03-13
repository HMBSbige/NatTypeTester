namespace STUN.Enums;

/// <summary>
/// Represents the NAT filtering behavior as defined in RFC 5780.
/// </summary>
public enum FilteringBehavior
{
	/// <summary>
	/// The filtering behavior is unknown or has not been determined.
	/// </summary>
	Unknown,

	/// <summary>
	/// The STUN server does not support the required features for the filtering test.
	/// </summary>
	UnsupportedServer,

	/// <summary>
	/// Endpoint-Independent Filtering: the NAT allows packets from any source address and port.
	/// </summary>
	EndpointIndependent,

	/// <summary>
	/// Address-Dependent Filtering: the NAT only allows packets from the same source address the endpoint previously sent to.
	/// </summary>
	AddressDependent,

	/// <summary>
	/// Address and Port-Dependent Filtering: the NAT only allows packets from the same source address and port the endpoint previously sent to.
	/// </summary>
	AddressAndPortDependent,

	/// <summary>
	/// Filtering test applies only to UDP.
	/// </summary>
	None
}
