namespace NatTypeTester.Domain.Shared.Localization;

[LocalizedConfiguration
(
	Default = "en",
	EnsureKeysIdentical = true,
	GenerationMode = GenerationMode.Compiled,
	NotificationMode = NotificationMode.CurrentCulturePropertyChanged
)]
public static partial class NatTypeTesterLanguage
{
	public static void SetCurrent(CultureInfo culture)
	{
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		SetCurrent(culture.Name);
	}
}
