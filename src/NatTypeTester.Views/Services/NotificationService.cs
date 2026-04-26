namespace NatTypeTester.Views.Services;

[UsedImplicitly]
internal class NotificationService : INotificationService, ISingletonDependency
{
	private (TopLevel TopLevel, WindowNotificationManager Manager)? _notificationManagerCache;

	private WindowNotificationManager? NotificationManager
	{
		get
		{
			TopLevel? topLevel = TopLevelHelper.GetTopLevel();
			if (topLevel is null)
			{
				return null;
			}

			if (_notificationManagerCache is { } cache && cache.TopLevel == topLevel)
			{
				return cache.Manager;
			}

			WindowNotificationManager manager = CreateNotificationManager(topLevel);
			_notificationManagerCache = (topLevel, manager);
			return manager;
		}
	}

	private static WindowNotificationManager CreateNotificationManager(TopLevel topLevel)
	{
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
