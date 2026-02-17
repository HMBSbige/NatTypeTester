namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public sealed class ObservableCultureService : ISingletonDependency, IDisposable
{
	public required IStringLocalizer<NatTypeTesterResource> L { get; init; }

	public IObservable<Unit> CultureChanged => _observableCultureChanged;

	private readonly BehaviorSubject<Unit> _observableCultureChanged = new(default);

	public void ChangeCulture(CultureInfo culture)
	{
		CultureInfo.DefaultThreadCurrentCulture = culture;
		CultureInfo.DefaultThreadCurrentUICulture = culture;
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		_observableCultureChanged.OnNext(default);
	}

	public void Dispose()
	{
		_observableCultureChanged.Dispose();
	}
}
