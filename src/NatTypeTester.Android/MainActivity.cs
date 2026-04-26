using Android.Content.PM;
using Android.Runtime;
using Avalonia;
using Avalonia.Android;
using Avalonia.Media;
using JetBrains.Annotations;
using NatTypeTester.Views;
using NatTypeTester.Views.Infrastructure;
using SkiaSharp;

namespace NatTypeTester.Android;

[Activity
(
	Label = "NatTypeTester",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
)]
public class MainActivity : AvaloniaMainActivity
{
	public override void OnBackPressed()
	{
		MoveTaskToBack(true);
	}
}

[Application]
[UsedImplicitly]
public class AndroidApp(nint javaReference, JniHandleOwnership transfer) : AvaloniaAndroidApplication<App>(javaReference, transfer)
{
	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		return base.CustomizeAppBuilder(builder)
			.UseNatTypeTesterApp()
			.With(CreateFontManagerOptions())
			.LogToTrace();
	}

	private static FontManagerOptions CreateFontManagerOptions()
	{
		FontManagerOptions options = new();
		List<FontFallback> fontFallbacks = [];

		using SKTypeface? zhHans = SKFontManager.Default.MatchCharacter(string.Empty, ["zh-Hans"], '这');

		if (zhHans is not null)
		{
			fontFallbacks.Add(new FontFallback { FontFamily = new FontFamily(zhHans.FamilyName) });
		}

		if (fontFallbacks.Count > 0)
		{
			options.FontFallbacks = fontFallbacks;
		}

		return options;
	}
}
