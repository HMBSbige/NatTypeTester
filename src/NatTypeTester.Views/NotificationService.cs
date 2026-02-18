namespace NatTypeTester.Views;

[UsedImplicitly]
internal class NotificationService(MainWindow mainWindow) : INotificationService, ISingletonDependency
{
	public void Show(string title, string message, AppNotificationType type)
	{
		NotificationType notificationType = type switch
		{
			AppNotificationType.Success => NotificationType.Success,
			AppNotificationType.Warning => NotificationType.Warning,
			AppNotificationType.Error => NotificationType.Error,
			_ => NotificationType.Information
		};

		mainWindow.NotificationManager.Show(new Notification(title, message, notificationType));
	}
}
