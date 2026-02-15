namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public partial class RFC3489ViewModel : ViewModelBase, ISingletonDependency
{
	[Reactive]
	public partial NatType NatType { get; set; }

	[Reactive]
	public partial string? PublicEndPoint { get; set; }

	[Reactive]
	public partial string? LocalEnd { get; set; }

	[ObservableAsProperty]
	public partial bool IsTesting { get; }

	public RFC3489ViewModel()
	{
		_isTestingHelper = TestClassicNatTypeCommand.IsExecuting.ToProperty(this, x => x.IsTesting);
	}

	[ReactiveCommand]
	private async Task TestClassicNatTypeAsync(CancellationToken token)
	{
		IRfc3489AppService service = TransientCachedServiceProvider.GetRequiredService<IRfc3489AppService>();
		SettingsViewModel settings = TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();

		using (Observable.Interval(TimeSpan.FromSeconds(0.1))
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe
					(_ =>
						{
							if (service.State is { } state)
							{
								ApplyResult(state);
							}
						}
					))
		{
			ClassicStunResult result = await service.TestAsync
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
				token
			);

			ApplyResult(result);
		}
	}

	private void ApplyResult(ClassicStunResult result)
	{
		NatType = result.NatType;
		PublicEndPoint = result.PublicEndPoint?.ToString();
		LocalEnd = result.LocalEndPoint?.ToString();
	}
}
