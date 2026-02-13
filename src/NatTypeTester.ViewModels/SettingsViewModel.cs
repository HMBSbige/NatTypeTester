using System.Collections.ObjectModel;
using System.Globalization;
using Volo.Abp.Localization;

namespace NatTypeTester.ViewModels;

public record LanguageOption(string CultureName, string DisplayName);

[UsedImplicitly]
public partial class SettingsViewModel : ViewModelBase, ISingletonDependency
{
	public Config Config => TransientCachedServiceProvider.GetRequiredService<Config>();

	[Reactive]
	public partial ReadOnlyObservableCollection<LanguageOption> Languages { get; private set; }

	[Reactive]
	public partial LanguageOption? SelectedLanguage { get; set; }

	public void Initialize()
	{
		ILanguageProvider languageProvider = TransientCachedServiceProvider.GetRequiredService<ILanguageProvider>();
		IEnumerable<LanguageOption> languages = languageProvider.GetLanguagesAsync().GetAwaiter().GetResult()
			.Select(l => new LanguageOption(l.CultureName, l.DisplayName));

		LanguageOption followSystem = new(string.Empty, L["FollowSystem"]);
		Languages = new ReadOnlyObservableCollection<LanguageOption>(new ObservableCollection<LanguageOption>(languages.Prepend(followSystem)));

		SelectedLanguage = Languages.FirstOrDefault(l => l.CultureName == Config.Language)
			?? followSystem;

		ApplyCulture(Config.Language);

		this.WhenAnyValue(x => x.SelectedLanguage)
			.Skip(1)
			.WhereNotNull()
			.Subscribe(lang =>
			{
				Config.Language = lang.CultureName;
				ApplyCulture(Config.Language);
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
