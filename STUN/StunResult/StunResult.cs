#if ReactiveUI
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
#endif
using System.Net;

namespace STUN.StunResult
{
	public abstract class StunResult
#if ReactiveUI
		: ReactiveObject
#endif
	{
#if ReactiveUI
		[Reactive]
#endif
		public IPEndPoint? PublicEndPoint { get; set; }

#if ReactiveUI
		[Reactive]
#endif
		public IPEndPoint? LocalEndPoint { get; set; }
	}
}
