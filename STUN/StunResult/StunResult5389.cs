#if ReactiveUI
using ReactiveUI.Fody.Helpers;
#endif
using STUN.Enums;
using System.Net;

namespace STUN.StunResult
{
	public class StunResult5389 : StunResult
	{
#if ReactiveUI
		[Reactive]
#endif
		public IPEndPoint? OtherEndPoint { get; set; }

#if ReactiveUI
		[Reactive]
#endif
		public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;

#if ReactiveUI
		[Reactive]
#endif
		public MappingBehavior MappingBehavior { get; set; } = MappingBehavior.Unknown;

#if ReactiveUI
		[Reactive]
#endif
		public FilteringBehavior FilteringBehavior { get; set; } = FilteringBehavior.Unknown;

		public void Clone(StunResult5389 result)
		{
			PublicEndPoint = result.PublicEndPoint;
			LocalEndPoint = result.LocalEndPoint;
			OtherEndPoint = result.OtherEndPoint;
			BindingTestResult = result.BindingTestResult;
			MappingBehavior = result.MappingBehavior;
			FilteringBehavior = result.FilteringBehavior;
		}

		public void Reset()
		{
			PublicEndPoint = default;
			LocalEndPoint = default;
			OtherEndPoint = default;
			BindingTestResult = BindingTestResult.Unknown;
			MappingBehavior = MappingBehavior.Unknown;
			FilteringBehavior = FilteringBehavior.Unknown;
		}
	}
}
