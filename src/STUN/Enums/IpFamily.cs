namespace STUN.Enums;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5389#section-15.1
/// </summary>
public enum IpFamily : byte
{
	/// <summary>
	/// IPv4 address family (0x01).
	/// </summary>
	IPv4 = 0x01,

	/// <summary>
	/// IPv6 address family (0x02).
	/// </summary>
	IPv6 = 0x02
}
