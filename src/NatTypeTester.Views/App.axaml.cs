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
			ScheduleStartupTasks(serviceProvider);
		}
		else if (ApplicationLifetime is IActivityApplicationLifetime activity)
		{
			activity.MainViewFactory = () =>
			{
				Control mainView = TopLevelHelper.RegisterActivityMainView(serviceProvider.GetRequiredService<MainView>());
				ScheduleStartupTasks(serviceProvider);
				return mainView;
			};
		}
		else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
		{
			singleViewPlatform.MainView = serviceProvider.GetRequiredService<MainView>();
			ScheduleStartupTasks(serviceProvider);
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static async void ScheduleStartupTasks(IServiceProvider serviceProvider)
	{
		MainWindowViewModel mainViewModel = serviceProvider.GetRequiredService<MainWindowViewModel>();
		await Dispatcher.UIThread.InvokeAsync(mainViewModel.RunStartupTasksAsync, DispatcherPriority.Loaded);
	}
}
