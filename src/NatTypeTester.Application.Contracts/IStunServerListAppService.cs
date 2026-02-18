namespace NatTypeTester.Application.Contracts;

public interface IStunServerListAppService : IApplicationService
{
	Task<List<string>> LoadAsync(LoadStunServerListInput input, CancellationToken cancellationToken = default);
}
