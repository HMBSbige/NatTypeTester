namespace NatTypeTester.Views.Extensions;

public class ObservableStringLocalizer(string key) : IObservable<string>
{
	private static readonly ObservableCultureService Service = Locator.Current.GetService<ObservableCultureService>()!;

	public IDisposable Subscribe(IObserver<string> observer)
	{
		return Service.CultureChanged.Subscribe(_ => observer.OnNext(Service.L[key]));
	}
}
