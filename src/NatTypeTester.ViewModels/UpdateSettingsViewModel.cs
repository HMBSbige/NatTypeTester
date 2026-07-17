namespace NatTypeTester.ViewModels;

public sealed partial class UpdateSettingsViewModel : ViewModelBase
{
	private bool _isInitialized;

	[Reactive]
	public partial bool AutoCheckUpdate { get; set; }

	[Reactive]
	public partial double CheckUpdateIntervalHours { get; set; }

	[Reactive]
	public partial bool IncludePreRelease { get; set; }

	[Reactive]
	public partial string? LatestVersion { get; set; }

	[Reactive]
	public partial string? CurrentVersion { get; set; }

	public UpdateSettingsViewModel()
	{
		CheckUpdateCommand.DisposeWith(Disposables);
		OpenHomepageCommand.DisposeWith(Disposables);
	}

	internal void ApplyConfig(AppConfig config)
	{
		if (_isInitialized)
		{
			return;
		}

		AutoCheckUpdate = config.AutoCheckUpdate;
		CheckUpdateIntervalHours = config.CheckUpdateInterval.TotalHours;
		IncludePreRelease = config.IncludePreRelease;
		CurrentVersion = AppLocator.Current.GetRequiredService<IUpdateAppService>().CurrentVersion;

		PersistToConfig
		(
			this.WhenAnyValue
			(
				static viewModel => viewModel.AutoCheckUpdate,
				static viewModel => viewModel.CheckUpdateIntervalHours,
				static viewModel => viewModel.IncludePreRelease,
				static (autoCheckUpdate, checkUpdateIntervalHours, includePreRelease) =>
					new UpdateConfigSnapshot(autoCheckUpdate, TimeSpan.FromHours(checkUpdateIntervalHours), includePreRelease)
			),
			static (appConfig, value) =>
			{
				appConfig.AutoCheckUpdate = value.AutoCheckUpdate;
				appConfig.CheckUpdateInterval = value.CheckUpdateInterval;
				appConfig.IncludePreRelease = value.IncludePreRelease;
			}
		);

		_isInitialized = true;
	}

	internal Task CheckForUpdateOnStartupAsync(AppConfig config, CancellationToken cancellationToken = default)
	{
#if DEBUG
		AppLocator.Current.GetRequiredService<INotificationService>().Show
		(
			NatTypeTesterLanguage.Current.Update,
			"[DEBUG] Checking for updates on startup."
		);
#endif

		if (!config.AutoCheckUpdate)
		{
			return Task.CompletedTask;
		}

		if (config.LastUpdateCheckTime is { } lastCheck && DateTimeOffset.Now - lastCheck < config.CheckUpdateInterval)
		{
			return Task.CompletedTask;
		}

		return CheckForUpdateCoreAsync(true, cancellationToken);
	}

	[ReactiveCommand]
	private async Task CheckUpdateAsync(CancellationToken cancellationToken = default)
	{
		await CheckForUpdateCoreAsync(false, cancellationToken);
	}

	private async Task CheckForUpdateCoreAsync(bool silentOnSuccess, CancellationToken cancellationToken)
	{
		ConnectionSettingsViewModel connectionSettings = AppLocator.Current.GetRequiredService<ConnectionSettingsViewModel>();
		IUpdateAppService updateAppService = AppLocator.Current.GetRequiredService<IUpdateAppService>();
		UpdateCheckInput input = new()
		{
			IncludePreRelease = IncludePreRelease,
			Proxy = connectionSettings.CreateProxyOptions()
		};
		UpdateCheckResult result = await updateAppService.CheckForUpdateAsync(input, cancellationToken);

		LatestVersion = result.LatestVersion;
		INotificationService notificationService = AppLocator.Current.GetRequiredService<INotificationService>();

		if (result is { HasUpdate: true, LatestVersion: not null })
		{
			notificationService.Show
			(
				NatTypeTesterLanguage.Current.Update,
				NatTypeTesterLanguage.Current.NewVersionAvailable.ToString(result.LatestVersion)
			);
		}
		else if (!silentOnSuccess)
		{
			notificationService.Show
			(
				NatTypeTesterLanguage.Current.Update,
				NatTypeTesterLanguage.Current.AlreadyLatestVersion,
				AppNotificationType.Success
			);
		}

		IAppConfigManager configManager = AppLocator.Current.GetRequiredService<IAppConfigManager>();
		await configManager.UpdateAsync(config => config.LastUpdateCheckTime = DateTimeOffset.Now, cancellationToken);
	}

	[ReactiveCommand]
	private async Task OpenHomepageAsync()
	{
		ILauncherService launcherService = AppLocator.Current.GetRequiredService<ILauncherService>();
		await launcherService.LaunchUriAsync(new Uri(NatTypeTesterConsts.HomepageUrl));
	}

	private readonly record struct UpdateConfigSnapshot(
		bool AutoCheckUpdate,
		TimeSpan CheckUpdateInterval,
		bool IncludePreRelease
	);
}
