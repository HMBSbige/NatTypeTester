namespace NatTypeTester.Application.Contracts;

public interface IUpdateAppService : IApplicationService
{
	string CurrentVersion { get; }

	Task<UpdateCheckResult> CheckForUpdateAsync(bool includePreRelease, CancellationToken cancellationToken = default);
}

public record UpdateCheckResult(bool HasUpdate, string? LatestVersion, string? ReleaseUrl);
