using NatTypeTester.Domain.Configuration;
using NuGet.Versioning;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using UpdateChecker;

namespace NatTypeTester.Application;

[UsedImplicitly]
public class UpdateAppService : ApplicationService, IUpdateAppService
{
	private IHttpClientFactory HttpClientFactory => LazyServiceProvider.GetRequiredService<IHttpClientFactory>();
	private IAppConfigManager AppConfigManager => LazyServiceProvider.GetRequiredService<IAppConfigManager>();

	private static readonly NuGetVersion CurrentAppVersion = ResolveCurrentVersion();

	public string CurrentVersion => CurrentAppVersion.ToNormalizedString();

	public async Task<UpdateCheckResult> CheckForUpdateAsync(bool includePreRelease, CancellationToken cancellationToken = default)
	{
		Dictionary<string, NuGetVersion> versionMap = new();
		string currentVersionKey = "__current__";
		versionMap[currentVersionKey] = CurrentAppVersion;

		Dictionary<string, string> tagVersionKeys = new(StringComparer.Ordinal);

		string? TagToComparableKey(string tag)
		{
			if (tagVersionKeys.TryGetValue(tag, out string? existingKey))
			{
				return existingKey;
			}

			NuGetVersion? version = TryParseTagVersion(tag);
			if (version is null)
			{
				return null;
			}

			string key = $"0.0.0.{tagVersionKeys.Count + 1}";
			tagVersionKeys[tag] = key;
			versionMap[key] = version;
			return key;
		}

		AppConfig appConfig = await AppConfigManager.GetAsync(cancellationToken);
		HttpProxyOptions proxyOptions = new(appConfig.ProxyType, appConfig.ProxyServer, appConfig.ProxyUser, appConfig.ProxyPassword);
		using HttpClient httpClient = AppHttpClientFactory.Create(HttpClientFactory, proxyOptions);
		httpClient.Timeout = TimeSpan.FromSeconds(15);
		httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "NatTypeTester");

		GitHubReleasesUpdateChecker checker = new(
			"HMBSbige",
			"NatTypeTester",
			includePreRelease,
			currentVersionKey,
			tag =>
			{
				string? key = TagToComparableKey(tag);
				return key ?? string.Empty;
			},
			new NuGetVersionKeyComparer(versionMap)
		);

		bool hasUpdate = await checker.CheckAsync(httpClient, cancellationToken);
		string? latestVersion = ResolveDisplayVersion(checker.LatestVersion, versionMap);

		return new UpdateCheckResult(
			hasUpdate,
			latestVersion,
			hasUpdate ? checker.LatestVersionUrl : null
		);
	}

	private static NuGetVersion ResolveCurrentVersion()
	{
		Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(UpdateAppService).Assembly;

		string? informationalVersion = assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
			?.InformationalVersion;

		if (NuGetVersion.TryParse(informationalVersion, out NuGetVersion? parsedInformationalVersion))
		{
			return parsedInformationalVersion;
		}

		string? fileVersion = assembly
			.GetCustomAttribute<AssemblyFileVersionAttribute>()
			?.Version;

		if (NuGetVersion.TryParse(fileVersion, out NuGetVersion? parsedFileVersion))
		{
			return parsedFileVersion;
		}

		Version? assemblyVersion = assembly.GetName().Version;
		return assemblyVersion is null ? new NuGetVersion(0, 0, 0) : new NuGetVersion(assemblyVersion);
	}

	private static NuGetVersion? TryParseTagVersion(string? tagName)
	{
		if (string.IsNullOrWhiteSpace(tagName))
		{
			return null;
		}

		string normalizedTag = tagName.Trim().TrimStart('v', 'V');
		return NuGetVersion.TryParse(normalizedTag, out NuGetVersion? version) ? version : null;
	}

	private static string? ResolveDisplayVersion(string? key, IReadOnlyDictionary<string, NuGetVersion> versionMap)
	{
		if (string.IsNullOrWhiteSpace(key))
		{
			return null;
		}

		return versionMap.TryGetValue(key, out NuGetVersion? version) ? version.ToNormalizedString() : key;
	}

	private sealed class NuGetVersionKeyComparer(IReadOnlyDictionary<string, NuGetVersion> versionMap) : IComparer<object>
	{
		public int Compare(object? x, object? y)
		{
			if (!TryGetVersion(x, out NuGetVersion? xVersion))
			{
				return TryGetVersion(y, out _) ? -1 : 0;
			}

			if (!TryGetVersion(y, out NuGetVersion? yVersion))
			{
				return 1;
			}

			return xVersion.CompareTo(yVersion);
		}

		private bool TryGetVersion(object? value, [NotNullWhen(true)] out NuGetVersion? version)
		{
			string? key = value?.ToString();
			if (string.IsNullOrWhiteSpace(key))
			{
				version = null;
				return false;
			}

			return versionMap.TryGetValue(key, out version);
		}
	}
}
