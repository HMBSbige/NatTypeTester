namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public partial class SettingsViewModel : ViewModelBase, ISingletonDependency
{
	[Reactive]
	public partial ProxyType ProxyType { get; set; }

	[Reactive]
	public partial string? ProxyServer { get; set; }

	[Reactive]
	public partial string? ProxyUser { get; set; }

	[Reactive]
	public partial string? ProxyPassword { get; set; }

	[ReactiveCollection]
	private ObservableCollection<LanguageOption> _languages;

	[Reactive]
	public partial LanguageOption? SelectedLanguage { get; set; }

	[Reactive]
	public partial string? StunServerListUri { get; set; }

	[Reactive]
	public partial bool AutoCheckUpdate { get; set; }

	[Reactive]
	public partial double CheckUpdateIntervalHours { get; set; }

	[Reactive]
	public partial bool IncludePreRelease { get; set; }

	[Reactive]
	public partial string? LatestVersionDisplay { get; set; }

	public string CurrentVersionDisplay => string.Format(L["CurrentVersion"], UpdateAppService.CurrentVersion);

	private IAppConfigManager AppConfigManager => TransientCachedServiceProvider.GetRequiredService<IAppConfigManager>();

	private INotificationService NotificationService => TransientCachedServiceProvider.GetRequiredService<INotificationService>();

	private IStunServerListAppService StunServerListAppService => TransientCachedServiceProvider.GetRequiredService<IStunServerListAppService>();

	private IUpdateAppService UpdateAppService => TransientCachedServiceProvider.GetRequiredService<IUpdateAppService>();

	public SettingsViewModel()
	{
		LoadStunServerListCommand.DisposeWith(Disposables);
		CheckUpdateCommand.DisposeWith(Disposables);
		OpenHomepageCommand.DisposeWith(Disposables);

		_languages = [];

		this.WhenAnyValue(x => x.SelectedLanguage)
			.Skip(1)
			.WhereNotNull()
			.Subscribe(lang => ApplyCulture(lang.CultureName), ex => RxState.DefaultExceptionHandler.OnNext(ex))
			.DisposeWith(Disposables);
	}

	internal async Task InitializeAsync(AppConfig config)
	{
		_languages.Add(new LanguageOption(string.Empty, L["FollowSystem"]));

		ILanguageProvider languageProvider = TransientCachedServiceProvider.GetRequiredService<ILanguageProvider>();
		IReadOnlyList<LanguageInfo> languageInfos = await languageProvider.GetLanguagesAsync();

		foreach (LanguageInfo l in languageInfos)
		{
			_languages.Add(new LanguageOption(l.CultureName, l.DisplayName));
		}

		ProxyType = config.ProxyType;
		ProxyServer = config.ProxyServer;
		ProxyUser = config.ProxyUser;
		ProxyPassword = config.ProxyPassword;
		StunServerListUri = config.StunServerListUri;
		SelectedLanguage = Languages.FirstOrDefault(l => l.CultureName == config.Language) ?? Languages.FirstOrDefault();
		ApplyCulture(SelectedLanguage?.CultureName);

		AutoCheckUpdate = config.AutoCheckUpdate;
		CheckUpdateIntervalHours = config.CheckUpdateInterval.TotalHours;
		IncludePreRelease = config.IncludePreRelease;

		ObserveAndUpdateConfig
		(
			this.WhenAnyValue
			(
				x => x.ProxyType,
				x => x.ProxyServer,
				x => x.ProxyUser,
				x => x.ProxyPassword,
				x => x.SelectedLanguage,
				x => x.StunServerListUri
			),
			(appConfig, value) =>
			{
				appConfig.ProxyType = value.Item1;
				appConfig.ProxyServer = value.Item2;
				appConfig.ProxyUser = value.Item3;
				appConfig.ProxyPassword = value.Item4;
				appConfig.Language = value.Item5?.CultureName;
				appConfig.StunServerListUri = value.Item6;
			}
		);

		ObserveAndUpdateConfig
		(
			this.WhenAnyValue
			(
				x => x.AutoCheckUpdate,
				x => x.CheckUpdateIntervalHours,
				x => x.IncludePreRelease
			),
			(appConfig, value) =>
			{
				appConfig.AutoCheckUpdate = value.Item1;
				appConfig.CheckUpdateInterval = TimeSpan.FromHours(value.Item2);
				appConfig.IncludePreRelease = value.Item3;
			}
		);
	}

	private void ObserveAndUpdateConfig<T>(IObservable<T> source, Action<AppConfig, T> updateAction)
	{
		source
			.Skip(1)
			.DistinctUntilChanged()
			.Select
			(value => Observable.FromAsync
				(ct => AppConfigManager.UpdateAsync(cfg => updateAction(cfg, value), ct).AsTask())
				.Catch<Unit, Exception>
				(ex =>
					{
						RxState.DefaultExceptionHandler.OnNext(ex);
						return Observable.Empty<Unit>();
					}
				)
			)
			.Switch()
			.Subscribe()
			.DisposeWith(Disposables);
	}

	[ReactiveCommand]
	private async Task CheckUpdateAsync(CancellationToken cancellationToken = default)
	{
		await CheckForUpdateCoreAsync(false, cancellationToken);
	}

	internal async Task CheckForUpdateOnStartupAsync(AppConfig config, CancellationToken cancellationToken = default)
	{
		if (!config.AutoCheckUpdate)
		{
			return;
		}

		if (config.LastUpdateCheckTime is { } lastCheck)
		{
			TimeSpan elapsed = DateTimeOffset.Now - lastCheck;

			if (elapsed < config.CheckUpdateInterval)
			{
				return;
			}
		}

		await CheckForUpdateCoreAsync(true, cancellationToken);
	}

	private async Task CheckForUpdateCoreAsync(bool silent, CancellationToken cancellationToken = default)
	{
		try
		{
			UpdateCheckResult result = await UpdateAppService.CheckForUpdateAsync(IncludePreRelease, cancellationToken);

			await AppConfigManager.UpdateAsync(c => c.LastUpdateCheckTime = DateTimeOffset.Now, cancellationToken);

			LatestVersionDisplay = string.Format(L["LatestVersion"], result.LatestVersion);

			if (result.HasUpdate)
			{
				NotificationService.Show
				(
					L["Update"],
					string.Format(L["NewVersionAvailable"], result.LatestVersion)
				);
			}
			else if (!silent)
			{
				NotificationService.Show
				(
					L["Update"],
					L["AlreadyLatestVersion"],
					AppNotificationType.Success
				);
			}
		}
		catch (Exception ex)
		{
			NotificationService.Show
			(
				L["Update"],
				$"{L["CheckUpdateFailed"]}{Environment.NewLine}{ex.Message}",
				AppNotificationType.Warning
			);
		}
	}

	[ReactiveCommand]
	private void OpenHomepage()
	{
		using Process? _ = Process.Start(new ProcessStartInfo(NatTypeTesterConsts.HomepageUrl) { UseShellExecute = true });
	}

	[ReactiveCommand]
	private async Task LoadStunServerListAsync(CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(StunServerListUri))
		{
			return;
		}

		try
		{
			LoadStunServerListInput input = new()
			{
				Uri = StunServerListUri,
				ProxyType = ProxyType,
				ProxyServer = ProxyServer,
				ProxyUser = ProxyUser,
				ProxyPassword = ProxyPassword
			};

			List<string> validServers = await StunServerListAppService.LoadAsync(input, cancellationToken);

			if (validServers.Count is 0)
			{
				NotificationService.Show(L["StunServerList"], L["StunServerListEmpty"], AppNotificationType.Error);
				return;
			}

			TransientCachedServiceProvider.GetRequiredService<MainWindowViewModel>().ReplaceStunServers(validServers);

			NotificationService.Show(L["StunServerList"], string.Format(L["StunServerListLoaded"], validServers.Count), AppNotificationType.Success);
		}
		catch (Exception ex)
		{
			NotificationService.Show(L["Error"], ex.Message, AppNotificationType.Error);
		}
	}

	private void ApplyCulture(string? language)
	{
		CultureInfo culture = string.IsNullOrEmpty(language) ? CultureInfo.InstalledUICulture : new CultureInfo(language);

		TransientCachedServiceProvider.GetRequiredService<ObservableCultureService>().ChangeCulture(culture);
	}
}

public record LanguageOption(string CultureName, string DisplayName);
