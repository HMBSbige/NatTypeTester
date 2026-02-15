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
		string? PublicEndPoint = null,
		string? LocalEnd = null
	);

	private readonly Dictionary<TransportType, ResultSnapshot> _cachedResults = new();

	public RFC5780ViewModel()
	{
		_isTestingHelper = DiscoveryNatTypeCommand.IsExecuting.ToProperty(this, x => x.IsTesting);

		this.WhenAnyValue(x => x.TransportType).Subscribe(_ => ResetResult());
	}

	[ReactiveCommand]
	private async Task DiscoveryNatTypeAsync(CancellationToken token)
	{
		IRfc5780AppService service = TransientCachedServiceProvider.GetRequiredService<IRfc5780AppService>();
		SettingsViewModel settings = TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();

		TransportType transport = TransportType;

		using (Observable.Interval(TimeSpan.FromSeconds(0.1))
					.ObserveOn(RxApp.MainThreadScheduler)
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
					StunServer = settings.StunServer,
					ProxyType = settings.ProxyType,
					ProxyServer = settings.ProxyServer,
					ProxyUser = settings.ProxyUser,
					ProxyPassword = settings.ProxyPassword,
					LocalEndPoint = LocalEnd
				},
				transport,
				token
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

	public void ResetResult()
	{
		ApplySnapshot(_cachedResults.GetValueOrDefault(TransportType));
	}
}
