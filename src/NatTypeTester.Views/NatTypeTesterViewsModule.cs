global using Avalonia;
global using Avalonia.Controls;
global using Avalonia.Controls.ApplicationLifetimes;
global using Avalonia.Controls.Notifications;
global using Avalonia.Controls.Primitives;
global using Avalonia.Data;
global using Avalonia.Markup.Xaml;
global using JetBrains.Annotations;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Localization;
global using NatTypeTester.Application.Contracts;
global using NatTypeTester.Configuration;
global using NatTypeTester.Domain.Shared;
global using NatTypeTester.Domain.Shared.Localization;
global using NatTypeTester.ViewModels;
global using NatTypeTester.ViewModels.Services;
global using NatTypeTester.Views.Infrastructure;
global using NatTypeTester.Views.Localization;
global using NatTypeTester.Views.Views;
global using ReactiveUI;
global using ReactiveUI.Avalonia;
global using Serilog;
global using Serilog.Core;
global using Serilog.Debugging;
global using Serilog.Events;
global using Splat;
global using Splat.Serilog;
global using STUN.Enums;
global using System.Diagnostics;
global using System.Globalization;
global using System.Reactive.Disposables.Fluent;
global using System.Reactive.Linq;
global using System.Reactive.Subjects;
global using Volo.Abp;
global using Volo.Abp.Autofac;
global using Volo.Abp.DependencyInjection;
global using Volo.Abp.Modularity;
global using Volo.Abp.Validation;

namespace NatTypeTester.Views;

[DependsOn
(
	typeof(AbpAutofacModule),
	typeof(NatTypeTesterViewModelsModule),
	typeof(NatTypeTesterConfigurationModule)
)]
[UsedImplicitly]
public class NatTypeTesterViewsModule : AbpModule
{
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		ConfigureLogging(context);
	}

	private static void ConfigureLogging(ServiceConfigurationContext context)
	{
#if DEBUG
		SelfLog.Enable
		(msg =>
			{
				Debug.Print(msg);
				Debugger.Break();
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

		AppLocator.CurrentMutable.UseSerilogFullLogger(logger);

		context.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(logger, true));
	}

	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		NotificationExceptionHandler.Install(context.ServiceProvider);
	}
}
