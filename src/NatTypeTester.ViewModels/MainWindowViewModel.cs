namespace NatTypeTester.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, IActivatableViewModel, ISingletonDependency
{
	public ViewModelActivator Activator { get; } = new();

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
		AddStunServerCommand.DisposeWith(Disposables);
		DeleteStunServerCommand.DisposeWith(Disposables);

		_stunServerSource.DisposeWith(Disposables);

		_stunServerSource.Connect()
			.Bind(out _stunServers)
			.Subscribe()
			.DisposeWith(Disposables);

		this.WhenActivated
		(disposables =>
			{
				Observable.FromAsync(cancellationToken => SettingsViewModel.CheckForUpdateOnStartupAsync(cancellationToken))
					.Catch<Unit, Exception>
					(ex =>
						{
							RxState.DefaultExceptionHandler.OnNext(ex);
							return Observable.Empty<Unit>();
						}
					)
					.Subscribe()
					.DisposeWith(disposables);
			}
		);
	}

	[ReactiveCommand]
	private void AddStunServer()
	{
		if (!StunServer.TryParse(CurrentStunServer, out StunServer? server))
		{
			return;
		}

		CurrentStunServer = server.ToString();

		if (_stunServerSource.Items.Any(s => string.Equals(s, CurrentStunServer, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}

		_stunServerSource.Add(CurrentStunServer);
	}

	[ReactiveCommand]
	private void DeleteStunServer()
	{
		int index = _stunServerSource.Items.IndexOf
		(
			_stunServerSource.Items.FirstOrDefault(s => string.Equals(s, CurrentStunServer, StringComparison.OrdinalIgnoreCase))
		);

		if (index < 0)
		{
			return;
		}

		_stunServerSource.RemoveAt(index);

		if (_stunServerSource.Count > 0)
		{
			CurrentStunServer = index < _stunServerSource.Count
				? _stunServerSource.Items[index]
				: _stunServerSource.Items[^1];
		}
		else
		{
			CurrentStunServer = string.Empty;
		}
	}

	public void ReplaceStunServers(IEnumerable<string> servers)
	{
		_stunServerSource.Edit
		(list =>
			{
				list.Clear();
				list.AddRange(servers);
			}
		);
	}

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		IAppConfigManager configManager = TransientCachedServiceProvider.GetRequiredService<IAppConfigManager>();
		AppConfig config = await configManager.GetAsync(cancellationToken);

		await SettingsViewModel.InitializeAsync(config);

		_stunServerSource.AddRange(config.StunServers);

		string? savedServer = config.CurrentStunServer;
		CurrentStunServer = string.IsNullOrEmpty(savedServer) ? _stunServerSource.Items[0] : savedServer;

		this.WhenAnyValue(x => x.CurrentStunServer)
			.Skip(1)
			.DistinctUntilChanged()
			.Select
			(value => Observable.FromAsync(ct => configManager.UpdateAsync(cfg => cfg.CurrentStunServer = value, ct).AsTask())
				.Catch<Unit, Exception>
				(ex =>
					{
						RxState.DefaultExceptionHandler.OnNext(ex);
						return Observable.Empty<Unit>();
					}
				)
			)
			.Switch()
			.Subscribe()
			.DisposeWith(Disposables);

		_stunServerSource.Connect()
			.Skip(1)
			.Select
			(_ => Observable.FromAsync(ct => configManager.UpdateAsync(cfg => cfg.StunServers = _stunServerSource.Items.ToList(), ct).AsTask())
				.Catch<Unit, Exception>
				(ex =>
					{
						RxState.DefaultExceptionHandler.OnNext(ex);
						return Observable.Empty<Unit>();
					}
				)
			)
			.Switch()
			.Subscribe()
			.DisposeWith(Disposables);
	}
}
