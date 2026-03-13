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

	private CancellationTokenSource? _cts;

	public RFC5780ViewModel()
	{
		DiscoveryNatTypeCommand.DisposeWith(Disposables);
		CancelTestCommand.DisposeWith(Disposables);

		_isTestingHelper = DiscoveryNatTypeCommand.IsExecuting.ToProperty(this, x => x.IsTesting).DisposeWith(Disposables);

		this.WhenAnyValue(x => x.TransportType)
			.Subscribe(transportType => ApplySnapshot(_cachedResults.GetValueOrDefault(transportType)))
			.DisposeWith(Disposables);
	}

	[ReactiveCommand]
	private void CancelTest()
	{
		_cts?.Cancel();
	}

	[ReactiveCommand]
	private async Task DiscoveryNatTypeAsync(StunTestType testType, CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_cts = cts;

		try
		{
			IRfc5780AppService service = TransientCachedServiceProvider.GetRequiredService<IRfc5780AppService>();
			SettingsViewModel settings = TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();
			MainWindowViewModel mainWindowViewModel = TransientCachedServiceProvider.GetRequiredService<MainWindowViewModel>();

			TransportType transport = TransportType;

			StunTestInput input = new()
			{
				StunServer = mainWindowViewModel.CurrentStunServer,
				ProxyType = settings.ProxyType,
				ProxyServer = settings.ProxyServer,
				ProxyUser = settings.ProxyUser,
				ProxyPassword = settings.ProxyPassword,
				LocalEndPoint = LocalEnd,
				SkipCertificateValidation = settings.SkipCertificateValidation
			};

			using (Observable.Interval(TimeSpan.FromSeconds(0.1))
						.Subscribe
						(_ =>
							{
								if (service.State is { } state)
								{
									ApplyAndCacheResult(state, transport, testType);
								}
							}
						))
			{
				StunResult5389 result = await (testType switch
				{
					StunTestType.Binding => service.BindingTestAsync(input, transport, cts.Token),
					StunTestType.Mapping => service.MappingBehaviorTestAsync(input, transport, cts.Token),
					StunTestType.Filtering => service.FilteringBehaviorTestAsync(input, transport, cts.Token),
					_ => service.TestAsync(input, transport, cts.Token)
				});

				ApplyAndCacheResult(result, transport, testType);
			}
		}
		finally
		{
			_cts = null;
		}
	}

	private void ApplyAndCacheResult(StunResult5389 result, TransportType transport, StunTestType testType)
	{
		ResultSnapshot existing = _cachedResults.GetValueOrDefault(transport);
		ResultSnapshot snapshot = testType switch
		{
			StunTestType.Binding => existing with
			{
				BindingTestResult = result.BindingTestResult,
				PublicEndPoint = result.PublicEndPoint?.ToString(),
				LocalEnd = result.LocalEndPoint?.ToString()
			},
			StunTestType.Mapping => existing with
			{
				MappingBehavior = result.MappingBehavior,
				PublicEndPoint = result.PublicEndPoint?.ToString(),
				LocalEnd = result.LocalEndPoint?.ToString()
			},
			StunTestType.Filtering => existing with
			{
				FilteringBehavior = result.FilteringBehavior,
				PublicEndPoint = result.PublicEndPoint?.ToString(),
				LocalEnd = result.LocalEndPoint?.ToString()
			},
			_ => new ResultSnapshot
			(
				result.BindingTestResult,
				result.MappingBehavior,
				result.FilteringBehavior,
				result.PublicEndPoint?.ToString(),
				result.LocalEndPoint?.ToString()
			)
		};
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
