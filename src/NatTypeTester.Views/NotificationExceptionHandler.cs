namespace NatTypeTester.Views;

public static class NotificationExceptionHandler
{
	public static readonly Subject<Exception> ExceptionSubject = new();

	public static void Install(IServiceProvider serviceProvider)
	{
		INotificationService notificationService = serviceProvider.GetRequiredService<INotificationService>();
		IStringLocalizer localizer = serviceProvider.GetRequiredService<IStringLocalizer<NatTypeTesterResource>>();

		ExceptionSubject
			.ObserveOn(RxSchedulers.MainThreadScheduler)
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
