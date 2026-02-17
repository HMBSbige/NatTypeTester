namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public partial class RFC5780ViewModel : ViewModelBase, ISingletonDependency
{
	[Reactive]
	public partial BindingTestResult BindingTestResult { get; set; }

	[Reactive]
	public partial MappingBehavior MappingBehavior { get; set; }

	[Reactive]
	public partial FilteringBehavior FilteringBehavior { get; set; }

	[Reactive]
	public partial string? PublicEndPoint { get; set; }

	[Reactive]
	public partial string? LocalEnd { get; set; }

	[Reactive]
	public partial TransportType TransportType { get; set; }

	[ObservableAsProperty]
	public partial bool IsTesting { get; }

	private readonly record struct ResultSnapshot(
		BindingTestResult BindingTestResult = default,
		MappingBehavior MappingBehavior = default,
		FilteringBehavior FilteringBehavior = default,
		string? PublicEndPoint = default,
		string? LocalEnd = default
	);

	private readonly Dictionary<TransportType, ResultSnapshot> _cachedResults = new();

	public RFC5780ViewModel()
	{
		_isTestingHelper = DiscoveryNatTypeCommand.IsExecuting.ToProperty(this, x => x.IsTesting).DisposeWith(Disposables);

		this.WhenAnyValue(x => x.TransportType)
			.Subscribe(transportType => ApplySnapshot(_cachedResults.GetValueOrDefault(transportType)))
			.DisposeWith(Disposables);
	}

	[ReactiveCommand]
	private async Task DiscoveryNatTypeAsync(CancellationToken cancellationToken = default)
	{
		IRfc5780AppService service = TransientCachedServiceProvider.GetRequiredService<IRfc5780AppService>();
		SettingsViewModel settings = TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();
		MainWindowViewModel mainWindowViewModel = TransientCachedServiceProvider.GetRequiredService<MainWindowViewModel>();

		TransportType transport = TransportType;

		using (Observable.Interval(TimeSpan.FromSeconds(0.1))
					.Subscribe
					(_ =>
						{
							if (service.State is { } state)
							{
								ApplyAndCacheResult(state, transport);
							}
						}
					))
		{
			StunResult5389 result = await service.TestAsync
			(
				new StunTestInput
				{
					StunServer = mainWindowViewModel.CurrentStunServer,
					ProxyType = settings.ProxyType,
					ProxyServer = settings.ProxyServer,
					ProxyUser = settings.ProxyUser,
					ProxyPassword = settings.ProxyPassword,
					LocalEndPoint = LocalEnd
				},
				transport,
				cancellationToken
			);

			ApplyAndCacheResult(result, transport);
		}
	}

	private void ApplyAndCacheResult(StunResult5389 result, TransportType transport)
	{
		ResultSnapshot snapshot = new
		(
			result.BindingTestResult,
			result.MappingBehavior,
			result.FilteringBehavior,
			result.PublicEndPoint?.ToString(),
			result.LocalEndPoint?.ToString()
		);
		_cachedResults[transport] = snapshot;
		ApplySnapshot(snapshot);
	}

	private void ApplySnapshot(ResultSnapshot snapshot)
	{
		BindingTestResult = snapshot.BindingTestResult;
		MappingBehavior = snapshot.MappingBehavior;
		FilteringBehavior = snapshot.FilteringBehavior;
		PublicEndPoint = snapshot.PublicEndPoint;
		LocalEnd = snapshot.LocalEnd;
	}
}
