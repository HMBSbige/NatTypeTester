namespace NatTypeTester.Views.Services;

[UsedImplicitly]
internal class NotificationService : INotificationService, ISingletonDependency
{
	private WindowNotificationManager? NotificationManager => field ??= CreateNotificationManager();

	private static WindowNotificationManager? CreateNotificationManager()
	{
		if (TopLevelHelper.GetTopLevel() is not { } topLevel)
		{
			return null;
		}

		WindowNotificationManager manager = new(topLevel)
		{
			Position = NotificationPosition.TopRight,
			MaxItems = 3
		};
		manager.ApplyTemplate();
		return manager;
	}

	public void Show(string title, string message, AppNotificationType type)
	{
		NotificationType notificationType = type switch
		{
			AppNotificationType.Success => NotificationType.Success,
			AppNotificationType.Warning => NotificationType.Warning,
			AppNotificationType.Error => NotificationType.Error,
			_ => NotificationType.Information
		};

		NotificationManager?.Show(new Notification(title, message, notificationType));
	}
}
