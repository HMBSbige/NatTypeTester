namespace NatTypeTester.Views;

public class App : Avalonia.Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		IServiceProvider serviceProvider = AppLocator.Current.GetRequiredService<IServiceProvider>();

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
		}
		else if (ApplicationLifetime is IActivityApplicationLifetime activity)
		{
			activity.MainViewFactory = () => TopLevelHelper.RegisterActivityMainView(serviceProvider.GetRequiredService<MainView>());
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			singleViewPlatform.MainView = serviceProvider.GetRequiredService<MainView>();
		}

		if (ApplicationLifetime is IControlledApplicationLifetime lifetime)
		{
			lifetime.Exit += (_, _) =>
			{
				using IAbpApplication app = serviceProvider.GetRequiredService<IAbpApplication>();
				app.Shutdown();
			};
		}

		base.OnFrameworkInitializationCompleted();
	}
}
