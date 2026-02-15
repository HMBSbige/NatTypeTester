namespace NatTypeTester.ViewModels;

public record LanguageOption(string CultureName, string DisplayName);

[UsedImplicitly]
public partial class SettingsViewModel : ViewModelBase, ISingletonDependency
{
	[Reactive]
	public partial string StunServer { get; set; }

	[Reactive]
	public partial ProxyType ProxyType { get; set; }

	[Reactive]
	public partial string ProxyServer { get; set; }

	[Reactive]
	public partial string? ProxyUser { get; set; }

	[Reactive]
	public partial string? ProxyPassword { get; set; }

	[Reactive]
	public partial string Language { get; set; }

	[Reactive]
	public partial ReadOnlyObservableCollection<LanguageOption>? Languages { get; private set; }

	[Reactive]
	public partial LanguageOption? SelectedLanguage { get; set; }

	public SettingsViewModel()
	{
		StunServer = string.Empty;
		ProxyType = ProxyType.Plain;
		ProxyServer = @"127.0.0.1:1080";
		Language = string.Empty;
	}

	public void Initialize()
	{
		ILanguageProvider languageProvider = TransientCachedServiceProvider.GetRequiredService<ILanguageProvider>();
		IEnumerable<LanguageOption> languages = languageProvider.GetLanguagesAsync().GetAwaiter().GetResult()
			.Select(l => new LanguageOption(l.CultureName, l.DisplayName));

		LanguageOption followSystem = new(string.Empty, L["FollowSystem"]);
		Languages = new ReadOnlyObservableCollection<LanguageOption>(new ObservableCollection<LanguageOption>(languages.Prepend(followSystem)));

		SelectedLanguage = Languages.FirstOrDefault(l => l.CultureName == Language)
			?? followSystem;

		ApplyCulture(Language);

		this.WhenAnyValue(x => x.SelectedLanguage)
			.Skip(1)
			.WhereNotNull()
			.Subscribe(lang =>
			{
				Language = lang.CultureName;
				ApplyCulture(Language);
			});
	}

	private void ApplyCulture(string? language)
	{
		CultureInfo culture = string.IsNullOrEmpty(language)
			? CultureInfo.InstalledUICulture
			: new CultureInfo(language);

		TransientCachedServiceProvider.GetRequiredService<ObservableCultureService>().ChangeCulture(culture);
	}
}
