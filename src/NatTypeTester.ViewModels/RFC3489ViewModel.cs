namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public partial class RFC3489ViewModel : ViewModelBase, ISingletonDependency
{
	[Reactive]
	public partial ClassicStunResult Result3489 { get; set; }

	[Reactive]
	public partial string? LocalEnd { get; set; }

	public RFC3489ViewModel()
	{
		Result3489 = new ClassicStunResult();

		this.WhenAnyValue(x => x.LocalEnd)
			.Subscribe(value =>
			{
				System.Net.IPEndPoint? newEndPoint = null;
				if (!string.IsNullOrWhiteSpace(value))
				{
					System.Net.IPEndPoint.TryParse(value, out newEndPoint);
				}
				if (!Equals(Result3489.LocalEndPoint, newEndPoint))
				{
					Result3489 = Result3489 with { LocalEndPoint = newEndPoint };
				}
			});

		this.WhenAnyValue(x => x.Result3489)
			.Select(r => r.LocalEndPoint?.ToString())
			.DistinctUntilChanged()
			.Subscribe(value => LocalEnd = value);
	}

	[ReactiveCommand]
	private async Task TestClassicNatTypeAsync(CancellationToken token)
	{
		IRfc3489AppService service = TransientCachedServiceProvider.GetRequiredService<IRfc3489AppService>();
		SettingsViewModel settings = TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();

		using (Observable.Interval(TimeSpan.FromSeconds(0.1))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(_ =>
			{
				if (service.State is { } state)
				{
					Result3489 = state;
				}
			}))
		{
			Result3489 = await service.TestAsync(settings.ToInput(), Result3489, token);
		}
	}
}
