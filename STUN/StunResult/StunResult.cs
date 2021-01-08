using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System.Net;

namespace STUN.StunResult
{
	public abstract class StunResult : ReactiveObject
	{
		[Reactive]
		public IPEndPoint? PublicEndPoint { get; set; }

		[Reactive]
		public IPEndPoint? LocalEndPoint { get; set; }
	}
}
