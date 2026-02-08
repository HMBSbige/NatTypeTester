global using Avalonia.Controls;
global using Avalonia.Controls.ApplicationLifetimes;
global using Avalonia.Markup.Xaml;
global using JetBrains.Annotations;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using NatTypeTester.Extensions;
global using NatTypeTester.ViewModels;
global using NatTypeTester.Views;
global using ReactiveUI;
global using ReactiveUI.Avalonia;
global using Serilog;
global using Serilog.Core;
global using Serilog.Events;
global using Splat;
global using Splat.Serilog;
global using System.Reactive.Disposables.Fluent;
global using Volo.Abp;
global using Volo.Abp.Autofac;
global using Volo.Abp.DependencyInjection;
global using Volo.Abp.Modularity;

namespace NatTypeTester;

[DependsOn
(
	typeof(AbpAutofacModule),
	typeof(NatTypeTesterViewModelsModule)
)]
[UsedImplicitly]
public class NatTypeTesterModule : AbpModule
{
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		ConfigureLogging(context);
	}

	private static void ConfigureLogging(ServiceConfigurationContext context)
	{
#if DEBUG
		Serilog.Debugging.SelfLog.Enable
		(msg =>
			{
				System.Diagnostics.Debug.Print(msg);
				System.Diagnostics.Debugger.Break();
			}
		);
#endif

		Logger logger = new LoggerConfiguration()
#if DEBUG
			.MinimumLevel.Debug()
#else
			.MinimumLevel.Information()
#endif
			.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
			.Enrich.FromLogContext()
#if DEBUG
			.WriteTo.Async(c => c.Debug(outputTemplate: @"[{Timestamp:O}] [{Level}] {Message:lj}{NewLine}{Exception}"))
#endif
			.CreateLogger();

		Locator.CurrentMutable.UseSerilogFullLogger(logger);

		context.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger, true));
	}

	public override void OnApplicationShutdown(ApplicationShutdownContext context)
	{
		context.ServiceProvider.GetService<ILoggerProvider>()?.Dispose();
	}
}
