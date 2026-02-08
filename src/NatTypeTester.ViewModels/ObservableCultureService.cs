using System.Globalization;

namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public class ObservableCultureService : ISingletonDependency
{
	public required IStringLocalizer<NatTypeTesterResource> L { get; init; }

	public IObservable<Unit> CultureChanged => _observableCultureChanged;

	private readonly System.Reactive.Subjects.BehaviorSubject<Unit> _observableCultureChanged = new(default);

	public void ChangeCulture(CultureInfo culture)
	{
		CultureInfo.CurrentCulture = culture;
		CultureInfo.CurrentUICulture = culture;
		_observableCultureChanged.OnNext(default);
	}
}
