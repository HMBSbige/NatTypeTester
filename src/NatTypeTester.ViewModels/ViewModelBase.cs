namespace NatTypeTester.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IDisposable
{
	protected CompositeDisposable Disposables { get; } = new();

	protected void PersistToConfig<T>(
		IObservable<T> source,
		Action<AppConfig, T> updateAction)
	{
		IAppConfigManager configManager = AppLocator.Current.GetRequiredService<IAppConfigManager>();

		source
			.Skip(1)
			.DistinctUntilChanged()
			.Select
			(value => HandleErrors
				(
					Observable.FromAsync
						(ct => configManager.UpdateAsync(config => updateAction(config, value), ct).AsTask())
				)
			)
			.Switch()
			.Subscribe()
			.DisposeWith(Disposables);
	}

	protected void Forget(Func<CancellationToken, Task> taskFactory)
	{
		HandleErrors(Observable.FromAsync(taskFactory)).Subscribe().DisposeWith(Disposables);
	}

	private static IObservable<T> HandleErrors<T>(IObservable<T> source)
	{
		return source
			.Catch<T, OperationCanceledException>(static _ => Observable.Empty<T>())
			.Catch<T, Exception>
			(exception =>
				{
					RxState.DefaultExceptionHandler.OnNext(exception);
					return Observable.Empty<T>();
				}
			);
	}

	public virtual void Dispose()
	{
		Disposables.Dispose();
		GC.SuppressFinalize(this);
	}
}
