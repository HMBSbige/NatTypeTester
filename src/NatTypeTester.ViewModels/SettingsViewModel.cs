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

	private IAppConfigManager AppConfigManager => TransientCachedServiceProvider.GetRequiredService<IAppConfigManager>();

	public SettingsViewModel()
	{
		_languages = [];

		this.WhenAnyValue(x => x.SelectedLanguage)
			.Skip(1)
			.WhereNotNull()
			.Subscribe(lang => ApplyCulture(lang.CultureName), ex => RxApp.DefaultExceptionHandler.OnNext(ex))
			.DisposeWith(Disposables);

		this.WhenAnyValue
			(
				x => x.ProxyType,
				x => x.ProxyServer,
				x => x.ProxyUser,
				x => x.ProxyPassword,
				x => x.SelectedLanguage
			)
			.Skip(1)
			.Throttle(TimeSpan.FromMilliseconds(500))
			.DistinctUntilChanged()
			.Select
			(value => Observable.FromAsync
				(ct => AppConfigManager.UpdateAsync
					(
						config =>
						{
							config.ProxyType = value.Item1;
							config.ProxyServer = value.Item2;
							config.ProxyUser = value.Item3;
							config.ProxyPassword = value.Item4;
							config.Language = value.Item5?.CultureName;
						},
						ct
					).AsTask()
				)
				.Catch<Unit, Exception>
				(ex =>
					{
						RxApp.DefaultExceptionHandler.OnNext(ex);
						return Observable.Empty<Unit>();
					}
				)
			)
			.Switch()
			.Subscribe()
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
		SelectedLanguage = Languages.FirstOrDefault(l => l.CultureName == config.Language) ?? Languages.FirstOrDefault();
		ApplyCulture(SelectedLanguage?.CultureName);
	}

	private void ApplyCulture(string? language)
	{
		CultureInfo culture = string.IsNullOrEmpty(language) ? CultureInfo.InstalledUICulture : new CultureInfo(language);

		TransientCachedServiceProvider.GetRequiredService<ObservableCultureService>().ChangeCulture(culture);
	}
}

public record LanguageOption(string CultureName, string DisplayName);
