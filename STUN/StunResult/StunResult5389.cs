using ReactiveUI.Fody.Helpers;
using STUN.Enums;
using System.Net;

namespace STUN.StunResult
{
	public class StunResult5389 : StunResult
	{
		[Reactive]
		public IPEndPoint? OtherEndPoint { get; set; }

		[Reactive]
		public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;

		[Reactive]
		public MappingBehavior MappingBehavior { get; set; } = MappingBehavior.Unknown;

		[Reactive]
		public FilteringBehavior FilteringBehavior { get; set; } = FilteringBehavior.Unknown;
	}
}
