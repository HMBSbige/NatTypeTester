namespace NatTypeTester.Application.Contracts;

public interface IRfc5780AppService : IApplicationService
{
	StunResult5389? State { get; }

	Task<StunResult5389> BindingTestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default);

	Task<StunResult5389> MappingBehaviorTestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default);

	Task<StunResult5389> FilteringBehaviorTestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default);

	Task<StunResult5389> TestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default);
}
