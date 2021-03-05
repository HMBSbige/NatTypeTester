#if ReactiveUI
using ReactiveUI.Fody.Helpers;
#endif
using STUN.Enums;

namespace STUN.StunResult
{
	public class ClassicStunResult : StunResult
	{
#if ReactiveUI
		[Reactive]
#endif
		public NatType NatType { get; set; } = NatType.Unknown;
	}
}
