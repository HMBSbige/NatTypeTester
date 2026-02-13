namespace NatTypeTester;

public class App : Avalonia.Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		IServiceProvider serviceProvider = Locator.Current.GetService<IServiceProvider>()!;

		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.Exit += (sender, e) => serviceProvider.GetRequiredService<IAbpApplication>().Shutdown();
			desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();

			// Initialize language settings
			SettingsViewModel settingsVm = serviceProvider.GetRequiredService<SettingsViewModel>();
			settingsVm.Initialize();

			// Load STUN servers
			MainWindowViewModel mainVm = serviceProvider.GetRequiredService<MainWindowViewModel>();
			mainVm.LoadStunServer();
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			singleViewPlatform.MainView = serviceProvider.GetRequiredService<MainView>();
		}

		base.OnFrameworkInitializationCompleted();
	}
}
