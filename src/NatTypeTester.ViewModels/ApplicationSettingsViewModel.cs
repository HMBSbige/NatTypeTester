namespace NatTypeTester.ViewModels;

public sealed partial class ApplicationSettingsViewModel : ViewModelBase
{
	private bool _isInitialized;

	public ObservableCollection<LanguageOptionViewModel> Languages { get; } = [];

	[Reactive]
	public partial LanguageOptionViewModel? SelectedLanguage { get; set; }

	public static bool CanOpenConfigDirectory => OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

	public ApplicationSettingsViewModel()
	{
		OpenConfigDirectoryCommand.DisposeWith(Disposables);

		this.WhenAnyValue(static viewModel => viewModel.SelectedLanguage)
			.Skip(1)
			.WhereNotNull()
			.Subscribe
			(
				language => ApplyCulture(language.CultureName),
				static exception => RxState.DefaultExceptionHandler.OnNext(exception)
			)
			.DisposeWith(Disposables);
	}

	internal void ApplyConfig(AppConfig config)
	{
		if (_isInitialized)
		{
			return;
		}

		Languages.Add(new LanguageOptionViewModel(string.Empty, string.Empty));

		foreach (string cultureName in NatTypeTesterLanguage.SupportedLanguageTags)
		{
			CultureInfo cultureInfo = new(cultureName);
			Languages.Add(new LanguageOptionViewModel(cultureInfo.Name, cultureInfo.NativeName));
		}

		SelectedLanguage = Languages.FirstOrDefault(language => language.CultureName == config.Language) ?? Languages[0];

		PersistToConfig
		(
			this.WhenAnyValue(static viewModel => viewModel.SelectedLanguage)
				.Select(static language => language?.CultureName),
			static (appConfig, value) => appConfig.Language = value
		);

		_isInitialized = true;
	}

	[ReactiveCommand]
	private async Task OpenConfigDirectoryAsync()
	{
		if (!CanOpenConfigDirectory)
		{
			return;
		}

		UriBuilder uriBuilder = new()
		{
			Scheme = Uri.UriSchemeFile,
			Host = string.Empty,
			Path = ConfigurationConsts.ConfigDirectory
		};
		ILauncherService launcherService = AppLocator.Current.GetRequiredService<ILauncherService>();
		await launcherService.LaunchUriAsync(uriBuilder.Uri);
	}

	private static void ApplyCulture(string? language)
	{
		CultureInfo culture = string.IsNullOrEmpty(language) ? CultureInfo.InstalledUICulture : new CultureInfo(language);
		NatTypeTesterLanguage.SetCurrent(culture);
	}
}
