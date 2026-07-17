namespace NatTypeTester.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
	public ApplicationSettingsViewModel Application => AppLocator.Current.GetRequiredService<ApplicationSettingsViewModel>();

	public ConnectionSettingsViewModel Connection => AppLocator.Current.GetRequiredService<ConnectionSettingsViewModel>();

	public StunServerSettingsViewModel StunServer => AppLocator.Current.GetRequiredService<StunServerSettingsViewModel>();

	public UpdateSettingsViewModel Update => AppLocator.Current.GetRequiredService<UpdateSettingsViewModel>();

	[Reactive]
	public partial bool IsInitialized { get; private set; }

	protected void LoadConfig()
	{
		Forget(LoadConfigAsync);
	}

	public void ApplyConfig(AppConfig config)
	{
		if (IsInitialized)
		{
			return;
		}

		Application.ApplyConfig(config);
		Connection.ApplyConfig(config);
		StunServer.ApplyConfig(config);
		Update.ApplyConfig(config);

		IsInitialized = true;
	}

	private async Task LoadConfigAsync(CancellationToken cancellationToken)
	{
		IAppConfigManager configManager = AppLocator.Current.GetRequiredService<IAppConfigManager>();
		AppConfig config = await configManager.GetAsync(cancellationToken);
		ApplyConfig(config);
	}
}
