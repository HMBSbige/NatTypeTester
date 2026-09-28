namespace NatTypeTester.ViewModels;

public abstract class ViewModelBase : ReactiveObject, IDisposable
{
	protected MultipleDisposable Disposables { get; } = new();

	protected void PersistToConfig<T>(
		IObservable<T> source,
		Action<AppConfig, T> updateAction)
	{
		IAppConfigManager configManager = AppLocator.Current.GetRequiredService<IAppConfigManager>();

		source
			.Skip(1)
			.Unique()
			.Map
			(value => HandleErrors
				(
					Signal.FromAsync
					(async cancellationToken =>
						{
							await configManager.UpdateAsync(config => updateAction(config, value), cancellationToken);
							return RxVoid.Default;
						}
					)
				)
			)
			.SwitchTo()
			.Subscribe()
			.DisposeWith(Disposables);
	}

	protected void Forget(Func<CancellationToken, Task> taskFactory)
	{
		HandleErrors
		(
			Signal.FromAsync
			(async cancellationToken =>
				{
					await taskFactory(cancellationToken);
					return RxVoid.Default;
				}
			)
		).Subscribe().DisposeWith(Disposables);
	}

	protected static IDisposable PollState<T>(Func<T?> getState, Action<T> apply) where T : class
	{
		return Signal.Every(TimeSpan.FromSeconds(0.1), RxSchedulers.TaskpoolScheduler)
			.ObserveOn(RxSchedulers.MainThreadScheduler)
			.Map(_ => getState())
			.KeepNotNull()
			.Subscribe(apply);
	}

	private static IObservable<T> HandleErrors<T>(IObservable<T> source)
	{
		return source.Recover
		(
			static exception =>
			{
				if (exception is not OperationCanceledException)
				{
					RxState.DefaultExceptionHandler.OnNext(exception);
				}

				return Signal.None<T>();
			}
		);
	}

	public virtual void Dispose()
	{
		Disposables.Dispose();
		GC.SuppressFinalize(this);
	}
}
