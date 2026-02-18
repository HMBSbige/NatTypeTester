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
		TestClassicNatTypeCommand.DisposeWith(Disposables);

		_isTestingHelper = TestClassicNatTypeCommand.IsExecuting.ToProperty(this, x => x.IsTesting).DisposeWith(Disposables);
	}

	[ReactiveCommand]
	private async Task TestClassicNatTypeAsync(CancellationToken cancellationToken = default)
	{
		IRfc3489AppService service = TransientCachedServiceProvider.GetRequiredService<IRfc3489AppService>();
		SettingsViewModel settings = TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();
		MainWindowViewModel mainWindowViewModel = TransientCachedServiceProvider.GetRequiredService<MainWindowViewModel>();

		using (Observable.Interval(TimeSpan.FromSeconds(0.1))
					.Subscribe
					(_ =>
						{
							if (service.State is { } state)
							{
								ApplyResult(state);
							}
						}
					)
			)
		{
			ClassicStunResult result = await service.TestAsync
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
				cancellationToken
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
