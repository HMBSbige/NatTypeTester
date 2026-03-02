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

	private IAppConfigManager AppConfigManager => TransientCachedServiceProvider.GetRequiredService<IAppConfigManager>();

	private INotificationService NotificationService => TransientCachedServiceProvider.GetRequiredService<INotificationService>();

	private IStunServerListAppService StunServerListAppService => TransientCachedServiceProvider.GetRequiredService<IStunServerListAppService>();

	public SettingsViewModel()
	{
		LoadStunServerListCommand.DisposeWith(Disposables);

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

		this.WhenAnyValue
			(
				x => x.ProxyType,
				x => x.ProxyServer,
				x => x.ProxyUser,
				x => x.ProxyPassword,
				x => x.SelectedLanguage,
				x => x.StunServerListUri
			)
			.Skip(1)
			.DistinctUntilChanged()
			.Select
			(value => Observable.FromAsync
				(ct => AppConfigManager.UpdateAsync
					(
						appConfig =>
						{
							appConfig.ProxyType = value.Item1;
							appConfig.ProxyServer = value.Item2;
							appConfig.ProxyUser = value.Item3;
							appConfig.ProxyPassword = value.Item4;
							appConfig.Language = value.Item5?.CultureName;
							appConfig.StunServerListUri = value.Item6;
						},
						ct
					).AsTask()
				)
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
