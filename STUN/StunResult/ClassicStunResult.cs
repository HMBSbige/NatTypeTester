using STUN.Enums;

namespace STUN.StunResult
{
	public class ClassicStunResult : StunResult
	{
		public NatType NatType { get; set; } = NatType.Unknown;

		public void Clone(ClassicStunResult result)
		{
			PublicEndPoint = result.PublicEndPoint;
			LocalEndPoint = result.LocalEndPoint;
			NatType = result.NatType;
		}

		public override void Reset()
		{
			base.Reset();
			NatType = NatType.Unknown;
		}
	}
}
