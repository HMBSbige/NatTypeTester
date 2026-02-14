namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public partial class RFC5780ViewModel : ViewModelBase, ISingletonDependency
{
	[Reactive]
	public partial StunResult5389 Result5389 { get; set; }

	[Reactive]
	public partial string? LocalEnd { get; set; }

	[Reactive]
	public partial TransportType TransportType { get; set; }

	[Reactive]
	public partial bool IsTesting { get; set; }

	private StunResult5389 _udpResult = new();
	private StunResult5389 _tcpResult = new();
	private StunResult5389 _tlsResult = new();

	public RFC5780ViewModel()
	{
		Result5389 = _udpResult;

		this.WhenAnyValue(x => x.LocalEnd)
			.Subscribe(value =>
			{
				System.Net.IPEndPoint? newEndPoint = null;
				if (!string.IsNullOrWhiteSpace(value))
				{
					System.Net.IPEndPoint.TryParse(value, out newEndPoint);
				}
				if (!Equals(Result5389.LocalEndPoint, newEndPoint))
				{
					Result5389 = Result5389 with { LocalEndPoint = newEndPoint };
				}
			});

		this.WhenAnyValue(x => x.Result5389)
			.Select(r => r.LocalEndPoint?.ToString())
			.DistinctUntilChanged()
			.Subscribe(value => LocalEnd = value);

		this.WhenAnyValue(x => x.TransportType)
			.Subscribe(_ => ResetResult());
	}

	[ReactiveCommand]
	private async Task DiscoveryNatTypeAsync(CancellationToken token)
	{
		IsTesting = true;
		try
		{
			IRfc5780AppService service = TransientCachedServiceProvider.GetRequiredService<IRfc5780AppService>();
			SettingsViewModel settings = TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();

			TransportType transport = TransportType;

			using (Observable.Interval(TimeSpan.FromSeconds(0.1))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ =>
				{
					if (service.State is { } state)
					{
						Result5389 = state;
						UpdateCachedResult(transport, state);
					}
				}))
			{
				StunResult5389 result = await service.TestAsync(settings.ToInput(), Result5389, transport, token);

				Result5389 = result;
				UpdateCachedResult(transport, result);
			}
		}
		finally
		{
			IsTesting = false;
		}
	}

	private void UpdateCachedResult(TransportType transport, StunResult5389 result)
	{
		switch (transport)
		{
			case TransportType.Udp:
				_udpResult = result;
				break;
			case TransportType.Tcp:
				_tcpResult = result;
				break;
			case TransportType.Tls:
				_tlsResult = result;
				break;
		}
	}

	public void ResetResult()
	{
		Result5389 = TransportType switch
		{
			TransportType.Tcp => _tcpResult,
			TransportType.Tls => _tlsResult,
			_ => _udpResult
		};
	}
}
