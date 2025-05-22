using STUN.Enums;
using System.Net;

namespace STUN.StunResult;

public record StunResult5389 : StunResult
{
	public IPEndPoint? OtherEndPoint { get; set; }

	public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;

	public MappingBehavior MappingBehavior { get; set; } = MappingBehavior.Unknown;

	public FilteringBehavior FilteringBehavior { get; set; } = FilteringBehavior.Unknown;
}
