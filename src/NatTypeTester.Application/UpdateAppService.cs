using NatTypeTester.Domain.Configuration;
using NatTypeTester.Domain.Shared;
using NuGet.Versioning;
using System.Reflection;
using UpdateChecker;

namespace NatTypeTester.Application;

[UsedImplicitly]
public class UpdateAppService : ApplicationService, IUpdateAppService
{
	private IHttpClientFactory HttpClientFactory => LazyServiceProvider.GetRequiredService<IHttpClientFactory>();

	private IAppConfigManager AppConfigManager => LazyServiceProvider.GetRequiredService<IAppConfigManager>();

	public string CurrentVersion => ResolveCurrentVersion()?.ToNormalizedString() ?? string.Empty;

	public async Task<UpdateCheckResult> CheckForUpdateAsync(bool includePreRelease, CancellationToken cancellationToken = default)
	{
		AppConfig appConfig = await AppConfigManager.GetAsync(cancellationToken);
		HttpProxyOptions proxyOptions = new(appConfig.ProxyType, appConfig.ProxyServer, appConfig.ProxyUser, appConfig.ProxyPassword);
		using HttpClient httpClient = AppHttpClientFactory.Create(HttpClientFactory, proxyOptions);
		httpClient.Timeout = TimeSpan.FromSeconds(15);
		httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd(nameof(NatTypeTester));

		GitHubReleasesUpdateChecker checker = new
		(
			NatTypeTesterConsts.Author,
			NatTypeTesterConsts.Repository,
			includePreRelease,
			CurrentVersion,
			tag => tag,
			new TagVersionComparer()
		);

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
		return TryParseVersion(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
	}

	private static NuGetVersion? TryParseVersion(string? value)
	{
		return NuGetVersion.TryParse(value, out NuGetVersion? version) ? version : default;
	}

	private sealed class TagVersionComparer : IComparer<object>
	{
		public int Compare(object? x, object? y)
		{
			NuGetVersion? xVersion = TryParseVersion(x?.ToString());
			NuGetVersion? yVersion = TryParseVersion(y?.ToString());
			return Comparer<NuGetVersion?>.Default.Compare(xVersion, yVersion);
		}
	}
}
