namespace NatTypeTester.Views.Localization;

public class ObservableStringLocalizer(string key) : IObservable<string>
{
	private static readonly ObservableCultureService Service = AppLocator.Current.GetService<ObservableCultureService>()!;

	public IDisposable Subscribe(IObserver<string> observer)
	{
		return Service.CultureChanged.Subscribe(_ => observer.OnNext(Service.L[key]));
	}
}
