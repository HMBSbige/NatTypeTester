using STUN.Enums;

namespace STUN.StunResult
{
	public class ClassicStunResult : StunResult
	{
		public NatType NatType { get; set; } = NatType.Unknown;

		public override void Reset()
		{
			base.Reset();
			NatType = NatType.Unknown;
		}
	}
}
