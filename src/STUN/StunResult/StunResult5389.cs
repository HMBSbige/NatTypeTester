using STUN.Enums;
using System.Net;

namespace STUN.StunResult;

/// <summary>
/// Represents the result of a STUN NAT behavior discovery test as defined in RFC 5389/5780.
/// Includes mapping behavior, filtering behavior, and binding test outcomes.
/// </summary>
public record StunResult5389 : StunResult
{
	/// <summary>
	/// Gets or sets the alternate server endpoint (OTHER-ADDRESS) reported by the STUN server.
	/// </summary>
	public IPEndPoint? OtherEndPoint { get; set; }

	/// <summary>
	/// Gets or sets the result of the STUN binding test.
	/// </summary>
	public BindingTestResult BindingTestResult { get; set; } = BindingTestResult.Unknown;

	/// <summary>
	/// Gets or sets the discovered NAT mapping behavior as defined in RFC 5780.
	/// </summary>
	public MappingBehavior MappingBehavior { get; set; } = MappingBehavior.Unknown;

	/// <summary>
	/// Gets or sets the discovered NAT filtering behavior as defined in RFC 5780.
	/// </summary>
	public FilteringBehavior FilteringBehavior { get; set; } = FilteringBehavior.Unknown;
}
