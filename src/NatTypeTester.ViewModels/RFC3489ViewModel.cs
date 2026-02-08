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
		StunClientAppService service = TransientCachedServiceProvider.GetRequiredService<StunClientAppService>();

		Result3489 = await service.TestClassicNatTypeAsync(
			Result3489,
			result => Result3489 = result,
			token);
	}
}
