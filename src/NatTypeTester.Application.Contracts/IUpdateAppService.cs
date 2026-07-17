namespace NatTypeTester.Application.Contracts;

public interface IUpdateAppService
{
	string CurrentVersion { get; }

	Task<UpdateCheckResult> CheckForUpdateAsync(UpdateCheckInput input, CancellationToken cancellationToken = default);
}

public sealed class UpdateCheckInput
{
	public bool IncludePreRelease { get; init; }

	public ProxyOptions Proxy { get; init; } = new();
}

public record UpdateCheckResult(bool HasUpdate, string? LatestVersion, string? ReleaseUrl);
