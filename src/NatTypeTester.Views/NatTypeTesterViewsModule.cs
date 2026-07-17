namespace NatTypeTester.Views;

public static class NatTypeTesterViewsServiceCollectionExtensions
{
	public static IServiceCollection AddNatTypeTesterViews(this IServiceCollection services)
	{
		services.AddNatTypeTesterConfiguration();
		services.AddNatTypeTesterApplication();
		services.AddNatTypeTesterViewModels();

		ConfigureLogging(services);

		services.TryAddSingleton<INotificationService, NotificationService>();
		services.TryAddSingleton<ILauncherService, LauncherService>();
		services.TryAddSingleton<NotificationExceptionHandler>();
		services.TryAddSingleton<MainWindow>(static serviceProvider => new MainWindow { DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>() });
		services.TryAddTransient<MainView>(static serviceProvider => new MainView { DataContext = serviceProvider.GetRequiredService<MainWindowViewModel>() });

		return services;
	}

	private static void ConfigureLogging(IServiceCollection services)
	{
#if DEBUG
		SelfLog.Enable
		(message =>
			{
				Debug.Print(message);
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
			.WriteTo.Async(sinks => sinks.Debug(outputTemplate: @"[{Timestamp:O}] [{Level}] {Message:lj}{NewLine}{Exception}"))
#endif
			.CreateLogger();

		AppLocator.CurrentMutable.UseSerilogFullLogger(logger);
		services.AddLogging(logging => logging.AddSerilog(logger, true));
	}
}
