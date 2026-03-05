namespace NatTypeTester.ViewModels.Extensions;

public static class ObservableExtensions
{
	extension<T>(IObservable<T> source)
	{
		public IObservable<T> CatchDefault()
		{
			return source.Catch<T, Exception>(ex =>
			{
				RxState.DefaultExceptionHandler.OnNext(ex);
				return Observable.Empty<T>();
			});
		}
	}

	extension(IActivatableViewModel viewModel)
	{
		public void WhenActivatedAsync(Func<CancellationToken, Task> asyncAction)
		{
			viewModel.WhenActivated(disposables =>
			{
				Observable.FromAsync(asyncAction)
					.CatchDefault()
					.Subscribe()
					.DisposeWith(disposables);
			});
		}
	}
}
