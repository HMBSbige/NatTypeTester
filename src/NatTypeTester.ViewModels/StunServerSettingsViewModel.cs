namespace NatTypeTester.ViewModels;

public partial class StunServerSettingsViewModel : ViewModelBase
{
	private readonly SourceList<string> _stunServerSource = new();

	[Reactive]
	public partial string CurrentStunServer { get; set; } = string.Empty;

	[Reactive]
	public partial string? StunServerListUri { get; set; }

	[Reactive]
	public partial bool IsInitialized { get; private set; }

	[BindableDerivedList]
	private readonly ReadOnlyObservableCollection<string> _stunServers;

	public StunServerSettingsViewModel()
	{
		AddStunServerCommand.DisposeWith(Disposables);
		DeleteStunServerCommand.DisposeWith(Disposables);
		LoadStunServerListCommand.DisposeWith(Disposables);

		_stunServerSource.DisposeWith(Disposables);
		_stunServerSource.Connect()
			.Bind(out _stunServers)
			.Subscribe()
			.DisposeWith(Disposables);
	}

	protected void LoadConfig()
	{
		Forget(LoadConfigAsync);
	}

	internal void ApplyConfig(AppConfig config)
	{
		if (IsInitialized)
		{
			return;
		}

		ReplaceStunServers(config.StunServers);
		CurrentStunServer = string.IsNullOrEmpty(config.CurrentStunServer) && _stunServerSource.Count > 0
			? _stunServerSource.Items[0]
			: config.CurrentStunServer ?? string.Empty;

		StunServerListUri = config.StunServerListUri;

		RegisterPersistence();
		IsInitialized = true;
	}

	[ReactiveCommand]
	private void AddStunServer()
	{
		if (!StunServer.TryParse(CurrentStunServer, out StunServer? server))
		{
			return;
		}

		CurrentStunServer = server.ToString();

		if (IndexOfCurrentStunServer() >= 0)
		{
			return;
		}

		_stunServerSource.Add(CurrentStunServer);
	}

	[ReactiveCommand]
	private void DeleteStunServer()
	{
		int index = IndexOfCurrentStunServer();

		if (index < 0)
		{
			return;
		}

		_stunServerSource.RemoveAt(index);

		CurrentStunServer = _stunServerSource.Count switch
		{
			0 => string.Empty,
			_ when index < _stunServerSource.Count => _stunServerSource.Items[index],
			_ => _stunServerSource.Items[^1]
		};
	}

	[ReactiveCommand]
	private async Task LoadStunServerListAsync(CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(StunServerListUri))
		{
			return;
		}

		ConnectionSettingsViewModel connectionSettings = AppLocator.Current.GetRequiredService<ConnectionSettingsViewModel>();
		IStunServerListAppService stunServerListAppService = AppLocator.Current.GetRequiredService<IStunServerListAppService>();
		LoadStunServerListInput input = new()
		{
			Uri = StunServerListUri,
			Proxy = connectionSettings.CreateProxyOptions()
		};
		List<string> validServers = await stunServerListAppService.LoadAsync(input, cancellationToken);
		INotificationService notificationService = AppLocator.Current.GetRequiredService<INotificationService>();

		if (validServers.Count is 0)
		{
			notificationService.Show
			(
				NatTypeTesterLanguage.Current.StunServerList,
				NatTypeTesterLanguage.Current.StunServerListEmpty,
				AppNotificationType.Error
			);
			return;
		}

		ReplaceStunServers(validServers);
		notificationService.Show
		(
			NatTypeTesterLanguage.Current.StunServerList,
			NatTypeTesterLanguage.Current.StunServerListLoaded.ToString(validServers.Count),
			AppNotificationType.Success
		);
	}

	private async Task LoadConfigAsync(CancellationToken cancellationToken)
	{
		IAppConfigManager configManager = AppLocator.Current.GetRequiredService<IAppConfigManager>();
		AppConfig config = await configManager.GetAsync(cancellationToken);
		ApplyConfig(config);
	}

	private void ReplaceStunServers(IEnumerable<string> servers)
	{
		_stunServerSource.Edit
		(list =>
			{
				list.Clear();
				list.AddRange(servers);
			}
		);
	}

	private void RegisterPersistence()
	{
		PersistToConfig
		(
			this.WhenAnyValue(static viewModel => viewModel.CurrentStunServer),
			static (appConfig, value) => appConfig.CurrentStunServer = value
		);

		PersistToConfig
		(
			_stunServerSource.Connect().ToCollection(),
			static (appConfig, value) => appConfig.StunServers = [.. value]
		);

		PersistToConfig
		(
			this.WhenAnyValue(static viewModel => viewModel.StunServerListUri),
			static (appConfig, value) => appConfig.StunServerListUri = value
		);
	}

	private int IndexOfCurrentStunServer()
	{
		return _stunServerSource.Items.IndexOf(CurrentStunServer, StringComparer.OrdinalIgnoreCase);
	}
}
