namespace NatTypeTester.ViewModels;

public partial class RFC3489ViewModel : ViewModelBase
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
		StunTestInput input = AppLocator.Current.CreateStunTestInput(LocalEnd);

		IRfc3489AppService service = AppLocator.Current.GetRequiredService<IRfc3489AppService>();

		using (PollState(() => service.State, ApplyResult))
		{
			ClassicStunResult result = await service.TestAsync(input, cancellationToken);
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
