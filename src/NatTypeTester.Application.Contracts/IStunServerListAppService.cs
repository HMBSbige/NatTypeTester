namespace NatTypeTester.Application.Contracts;

public interface IStunServerListAppService
{
	Task<List<string>> LoadAsync(LoadStunServerListInput input, CancellationToken cancellationToken = default);
}
