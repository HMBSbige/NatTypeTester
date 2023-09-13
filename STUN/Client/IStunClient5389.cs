using STUN.StunResult;

namespace STUN.Client;

public interface IStunClient5389 : IStunClient
{
	StunResult5389 State { get; }
	ValueTask<StunResult5389> BindingTestAsync(CancellationToken cancellationToken = default);
	ValueTask MappingBehaviorTestAsync(CancellationToken cancellationToken = default);
	ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default);
}
