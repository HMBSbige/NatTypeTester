using Android.Content.PM;
using Autofac.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Android;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using NatTypeTester.Views;
using NatTypeTester.Views.Infrastructure;
using ReactiveUI.Avalonia.Splat;
using SkiaSharp;
using Volo.Abp;

namespace NatTypeTester.Android;

[Activity
(
	Label = "NatTypeTester",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode
)]
public class MainActivity : AvaloniaMainActivity<App>
{
	public override void OnBackPressed()
	{
		MoveTaskToBack(true);
	}

	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		return base.CustomizeAppBuilder(builder)
			.UseReactiveUIWithAutofac
			(
				containerBuilder =>
				{
					ServiceCollection services = new();

					AbpApplicationFactory.Create<NatTypeTesterViewsModule>(services);

					containerBuilder.Populate(services);
				},
				withResolver: resolver =>
				{
					IServiceProvider serviceProvider = resolver.GetRequiredService<IServiceProvider>();
					resolver.GetRequiredService<IAbpApplicationWithExternalServiceProvider>().Initialize(serviceProvider);
				},
				withReactiveUIBuilder: rxBuilder =>
				{
					rxBuilder.WithExceptionHandler(NotificationExceptionHandler.ExceptionSubject);
				}
			)
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
