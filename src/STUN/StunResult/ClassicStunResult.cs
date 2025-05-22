using STUN.Enums;

namespace STUN.StunResult;

public record ClassicStunResult : StunResult
{
	public NatType NatType { get; set; } = NatType.Unknown;
}
