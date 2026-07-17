namespace NatTypeTester.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
	private bool _startupTasksStarted;

	public RFC3489ViewModel RFC3489ViewModel => AppLocator.Current.GetRequiredService<RFC3489ViewModel>();

	public RFC5780ViewModel RFC5780ViewModel => AppLocator.Current.GetRequiredService<RFC5780ViewModel>();

	public SettingsViewModel SettingsViewModel => AppLocator.Current.GetRequiredService<SettingsViewModel>();

	public async Task RunStartupTasksAsync()
	{
		if (_startupTasksStarted)
		{
			return;
		}

		_startupTasksStarted = true;

		IAppConfigManager configManager = AppLocator.Current.GetRequiredService<IAppConfigManager>();
		AppConfig config = await configManager.GetAsync();
		SettingsViewModel settingsViewModel = SettingsViewModel;

		settingsViewModel.ApplyConfig(config);
		Forget(cancellationToken => settingsViewModel.Update.CheckForUpdateOnStartupAsync(config, cancellationToken));
	}
}
