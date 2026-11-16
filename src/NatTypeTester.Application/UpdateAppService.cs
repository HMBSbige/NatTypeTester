namespace NatTypeTester.Application;

internal class UpdateAppService(IHttpClientFactory httpClientFactory) : IUpdateAppService
{
	public string CurrentVersion { get; } = Normalize(ThisAssembly.Info.InformationalVersion) ?? string.Empty;

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

		return new UpdateCheckResult(hasUpdate, checker.LatestVersion, checker.LatestVersionUrl);
	}

	private static string? Normalize(string value)
	{
		return NuGetVersion.TryParse(value, out NuGetVersion? version) ? version.ToNormalizedString() : null;
	}

	private sealed class NuGetVersionScheme : IVersionScheme
	{
		public bool TryParse(string value, out string version)
		{
			string? normalized = Normalize(value);
			version = normalized ?? string.Empty;
			return normalized is not null;
		}

		public int Compare(string x, string y)
		{
			return NuGetVersion.Parse(x).CompareTo(NuGetVersion.Parse(y));
		}
	}
}
