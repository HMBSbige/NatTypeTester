using STUN.StunResult;

namespace STUN.Client;

/// <summary>
/// Represents a STUN client implementing RFC 5389/5780 NAT behavior discovery tests.
/// </summary>
public interface IStunClient5389 : IStunClient
{
	/// <summary>
	/// Gets the current state of the STUN discovery result.
	/// </summary>
	StunResult5389 State { get; }

	/// <summary>
	/// Performs a STUN binding test to determine the public endpoint.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>The binding test result containing the mapped public endpoint.</returns>
	ValueTask<StunResult5389> BindingTestAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Performs a NAT mapping behavior test as defined in RFC 5780.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous test operation.</returns>
	ValueTask MappingBehaviorTestAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Performs a NAT filtering behavior test as defined in RFC 5780.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous test operation.</returns>
	ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default);
}
