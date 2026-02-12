using System.Globalization;

namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public partial class SettingsViewModel : ViewModelBase, ISingletonDependency
{
	public Config Config => TransientCachedServiceProvider.GetRequiredService<Config>();

	[Reactive]
	public partial int SelectedLanguageIndex { get; set; }

	public SettingsViewModel()
	{
		// Initialize language index based on current culture
		SelectedLanguageIndex = GetLanguageIndex(CultureInfo.CurrentUICulture);

		this.WhenAnyValue(x => x.SelectedLanguageIndex)
			.Skip(1) // Skip initial value
			.Subscribe(index =>
			{
				CultureInfo culture = index switch
				{
					1 => new CultureInfo("en"),
					2 => new CultureInfo("zh-CN"),
					_ => CultureInfo.InstalledUICulture // Follow system
				};

				Locator.Current.GetService<ObservableCultureService>()?.ChangeCulture(culture);
			});
	}

	private static int GetLanguageIndex(CultureInfo culture)
	{
		if (culture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
		{
			return 1;
		}

		if (culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
		{
			return 2;
		}

		return 0; // Follow system
	}
}
