namespace NatTypeTester.Application;

internal class UpdateAppService(IHttpClientFactory httpClientFactory) : IUpdateAppService
{
	public string CurrentVersion => ResolveCurrentVersion()?.ToNormalizedString() ?? string.Empty;

	public async Task<UpdateCheckResult> CheckForUpdateAsync(UpdateCheckInput input, CancellationToken cancellationToken = default)
	{
		using HttpClient httpClient = AppHttpClientFactory.Create(httpClientFactory, input.Proxy);
		httpClient.Timeout = TimeSpan.FromSeconds(15);
		httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(nameof(NatTypeTester));

		GitHubReleasesUpdateCheckerOptions options = new()
		{
			Owner = NatTypeTesterConsts.Author,
			Repo = NatTypeTesterConsts.Repository,
			IsPreRelease = input.IncludePreRelease,
			CurrentVersion = CurrentVersion,
			VersionScheme = new NuGetVersionScheme()
		};
		GitHubReleasesUpdateChecker checker = new(options);

		bool hasUpdate = await checker.CheckAsync(httpClient, cancellationToken);
		string? latestVersion = TryParseVersion(checker.LatestVersion)?.ToNormalizedString();

		return new UpdateCheckResult
		(
			hasUpdate,
			latestVersion,
			checker.LatestVersionUrl
		);
	}

	private static NuGetVersion? ResolveCurrentVersion()
	{
		return TryParseVersion(ThisAssembly.Info.InformationalVersion);
	}

	private static NuGetVersion? TryParseVersion(string? value)
	{
		return NuGetVersion.TryParse(value, out NuGetVersion? version) ? version : default;
	}

	private sealed class NuGetVersionScheme : IVersionScheme
	{
		public bool TryParse(string value, out string version)
		{
			NuGetVersion? nuGetVersion = TryParseVersion(value);
			version = nuGetVersion?.ToNormalizedString() ?? string.Empty;
			return nuGetVersion is not null;
		}

		public int Compare(string x, string y)
		{
			NuGetVersion? xVersion = TryParseVersion(x);
			NuGetVersion? yVersion = TryParseVersion(y);
			return Comparer<NuGetVersion?>.Default.Compare(xVersion, yVersion);
		}
	}
}
