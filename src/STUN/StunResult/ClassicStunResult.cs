using STUN.Enums;

namespace STUN.StunResult;

/// <summary>
/// Represents the result of a classic STUN NAT type discovery test as defined in RFC 3489.
/// </summary>
public record ClassicStunResult : StunResult
{
	/// <summary>
	/// Gets or sets the discovered NAT type according to RFC 3489 classification.
	/// </summary>
	public NatType NatType { get; set; } = NatType.Unknown;
}
