using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace NatTypeTester.Views.Infrastructure;

internal sealed class NotificationExceptionHandler(
	INotificationService notificationService,
	ILogger<NotificationExceptionHandler> logger
) : IDisposable
{
	private IDisposable? _subscription;

	public static Subject<Exception> ExceptionSubject { get; } = new();

	public void Install()
	{
		_subscription ??= ExceptionSubject
			.ObserveOn(RxSchedulers.MainThreadScheduler)
			.Subscribe(Handle);
	}

	public void Dispose()
	{
		_subscription?.Dispose();
	}

	private void Handle(Exception exception)
	{
		if (exception is OperationCanceledException)
		{
			logger.LogTrace(exception, "The operation was canceled.");
			return;
		}

		logger.LogError(exception, "An unhandled exception occurred during a operation.");
		notificationService.Show
		(
			NatTypeTesterLanguage.Current.Error,
			exception.Message,
			AppNotificationType.Error
		);
	}
}
