using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using STUN.Enums;
using System.Net;

namespace STUN.StunResult
{
	public class ClassicStunResult : ReactiveObject
	{
		[Reactive]
		public NatType NatType { get; set; } = NatType.Unknown;

		[Reactive]
		public IPEndPoint? PublicEndPoint { get; set; }
	}
}
