namespace NatTypeTester.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, ISingletonDependency
{
	public RFC3489ViewModel RFC3489ViewModel => TransientCachedServiceProvider.GetRequiredService<RFC3489ViewModel>();

	public RFC5780ViewModel RFC5780ViewModel => TransientCachedServiceProvider.GetRequiredService<RFC5780ViewModel>();

	public SettingsViewModel SettingsViewModel => TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();

	[Reactive]
	public partial string CurrentStunServer { get; set; } = string.Empty;

	[BindableDerivedList]
	private readonly ReadOnlyObservableCollection<string> _stunServers;

	private readonly SourceList<string> _stunServerSource = new();

	public MainWindowViewModel()
	{
		_stunServerSource.DisposeWith(Disposables);

		_stunServerSource.Connect()
			.Bind(out _stunServers)
			.Subscribe()
			.DisposeWith(Disposables);
	}

	public async Task InitializeAsync()
	{
		IAppConfigManager configManager = TransientCachedServiceProvider.GetRequiredService<IAppConfigManager>();
		AppConfig config = await configManager.GetAsync();

		await SettingsViewModel.InitializeAsync(config);

		_stunServerSource.AddRange(config.StunServers);

		string? savedServer = config.CurrentStunServer;
		CurrentStunServer = string.IsNullOrEmpty(savedServer) ? _stunServerSource.Items[0] : savedServer;

		this.WhenAnyValue(x => x.CurrentStunServer)
			.Skip(1)
			.Throttle(TimeSpan.FromMilliseconds(500))
			.DistinctUntilChanged()
			.Select
			(value => Observable.FromAsync(ct => configManager.UpdateAsync(cfg => cfg.CurrentStunServer = value, ct).AsTask())
				.Catch<Unit, Exception>
				(ex =>
					{
						RxApp.DefaultExceptionHandler.OnNext(ex);
						return Observable.Empty<Unit>();
					}
				)
			)
			.Switch()
			.Subscribe()
			.DisposeWith(Disposables);
	}
}
