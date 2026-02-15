namespace NatTypeTester.Application.Contracts;

public interface IRfc3489AppService : IApplicationService
{
	ClassicStunResult? State { get; }

	Task<ClassicStunResult> TestAsync(StunTestInput input, CancellationToken cancellationToken = default);
}
