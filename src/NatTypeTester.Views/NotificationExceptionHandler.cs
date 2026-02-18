namespace NatTypeTester.Views;

internal static class NotificationExceptionHandler
{
	private static readonly Subject<Exception> ExceptionSubject = new();

	public static void Install(IServiceProvider serviceProvider)
	{
		RxApp.DefaultExceptionHandler = ExceptionSubject;

		INotificationService notificationService = serviceProvider.GetRequiredService<INotificationService>();
		IStringLocalizer localizer = serviceProvider.GetRequiredService<IStringLocalizer<NatTypeTesterResource>>();

		ExceptionSubject
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe
			(ex =>
				{
					string message = FormatMessage(ex);
					notificationService.Show(localizer["Error"], message, AppNotificationType.Error);
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
