using Android.App;
using Android.Content.PM;
using Autofac.Extensions.DependencyInjection;
using Avalonia;
using Avalonia.Android;
using Microsoft.Extensions.DependencyInjection;
using NatTypeTester.Views;
using NatTypeTester.Views.Services;
using ReactiveUI.Avalonia.Splat;
using Volo.Abp;

namespace NatTypeTester.Android;

[Activity(
	Label = "NatTypeTester",
	Theme = "@style/MyTheme.NoActionBar",
	Icon = "@drawable/icon",
	MainLauncher = true,
	ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity<App>
{
	protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
	{
		return base.CustomizeAppBuilder(builder)
			.UseReactiveUIWithAutofac
			(
				builder =>
				{
					ServiceCollection services = new();

					AbpApplicationFactory.Create<NatTypeTesterViewsModule>(services);

					builder.Populate(services);
				},
				withResolver: resolver =>
				{
					IServiceProvider serviceProvider = resolver.GetService<IServiceProvider>()!;
					resolver.GetService<IAbpApplicationWithExternalServiceProvider>()!.Initialize(serviceProvider);
				},
				withReactiveUIBuilder: rxBuilder =>
				{
					rxBuilder.WithExceptionHandler(NotificationExceptionHandler.ExceptionSubject);
				}
			)
			.LogToTrace();
	}
}
