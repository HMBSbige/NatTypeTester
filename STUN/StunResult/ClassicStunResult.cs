using ReactiveUI.Fody.Helpers;
using STUN.Enums;

namespace STUN.StunResult
{
	public class ClassicStunResult : StunResult
	{
		[Reactive]
		public NatType NatType { get; set; } = NatType.Unknown;
	}
}
