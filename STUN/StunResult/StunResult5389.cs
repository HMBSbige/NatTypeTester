using STUN.Enums;
using System.Net;

namespace STUN.StunResult
{
	public class StunResult5389 : StunResult
	{
		public IPEndPoint? OtherEndPoint { get; set; }

		public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;

		public MappingBehavior MappingBehavior { get; set; } = MappingBehavior.Unknown;

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

		public override void Reset()
		{
			base.Reset();
			OtherEndPoint = default;
			BindingTestResult = BindingTestResult.Unknown;
			MappingBehavior = MappingBehavior.Unknown;
			FilteringBehavior = FilteringBehavior.Unknown;
		}
	}
}
