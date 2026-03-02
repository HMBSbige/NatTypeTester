namespace NatTypeTester.Views;

public class App : Avalonia.Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		IServiceProvider serviceProvider = AppLocator.Current.GetService<IServiceProvider>()!;

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			NotificationExceptionHandler.Install(serviceProvider);

			desktop.Exit += (_, _) =>
			{
				using IAbpApplication app = serviceProvider.GetRequiredService<IAbpApplication>();
				app.Shutdown();
			};

			desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			singleViewPlatform.MainView = serviceProvider.GetRequiredService<MainView>();
		}

		base.OnFrameworkInitializationCompleted();
	}
}
