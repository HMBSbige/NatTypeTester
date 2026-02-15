namespace NatTypeTester.Views;

internal static class NotificationExceptionHandler
{
	private static readonly Subject<Exception> ExceptionSubject = new();

	public static void Install(IServiceProvider serviceProvider)
	{
		RxApp.DefaultExceptionHandler = ExceptionSubject;

		MainWindow mainWindow = serviceProvider.GetRequiredService<MainWindow>();
		IStringLocalizer localizer = serviceProvider.GetRequiredService<IStringLocalizer<NatTypeTesterResource>>();

		ExceptionSubject
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe
			(ex =>
				{
					string message = FormatMessage(ex);
					mainWindow.NotificationManager?.Show(new Notification(localizer["Error"], message, NotificationType.Error));
				}
			);
	}

	private static string FormatMessage(Exception ex)
	{
		return ex switch
		{
			AbpValidationException { ValidationErrors.Count: > 0 } ve => string.Join(Environment.NewLine, ve.ValidationErrors.Select(e => e.ErrorMessage)),
			_ => ex.Message
		};
	}
}
