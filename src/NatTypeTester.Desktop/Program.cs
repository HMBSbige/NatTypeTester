using Autofac.Extensions.DependencyInjection;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI.Avalonia.Splat;
using Splat;
using Volo.Abp;

namespace NatTypeTester.Desktop;

internal static class Program
{
	/// <summary>
	/// Initialization code. Don't use any Avalonia, third-party APIs or any
	/// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	/// yet and stuff might break.
	/// </summary>
	[STAThread]
	public static int Main(string[] args)
	{
		return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	/// <summary>
	/// Avalonia configuration, don't remove; also used by visual designer.
	/// </summary>
	private static AppBuilder BuildAvaloniaApp()
	{
		AppBuilder builder = AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.UseReactiveUIWithAutofac
			(
				builder =>
				{
					ServiceCollection services = new();

					AbpApplicationFactory.Create<NatTypeTesterModule>(services);

					builder.Populate(services);
				},
				withResolver: resolver =>
				{
					IServiceProvider serviceProvider = resolver.GetService<IServiceProvider>()!;
					resolver.GetService<IAbpApplicationWithExternalServiceProvider>()!.Initialize(serviceProvider);
				}
			)
			.LogToTrace()
			.With(new Win32PlatformOptions { RenderingMode = [Win32RenderingMode.AngleEgl, Win32RenderingMode.Vulkan, Win32RenderingMode.Wgl, Win32RenderingMode.Software] })
			.With(new X11PlatformOptions { RenderingMode = [X11RenderingMode.Vulkan, X11RenderingMode.Egl, X11RenderingMode.Glx, X11RenderingMode.Software] });

		return builder;
	}
}
