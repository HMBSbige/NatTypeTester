namespace STUN.Enums;

public enum FilteringBehavior
{
	Unknown,
	UnsupportedServer,
	EndpointIndependent,
	AddressDependent,
	AddressAndPortDependent,

	/// <summary>
	/// Filtering test applies only to UDP.
	/// </summary>
	None
}
