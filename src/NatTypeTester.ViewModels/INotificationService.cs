namespace NatTypeTester.ViewModels;

public enum AppNotificationType
{
	Information,
	Success,
	Warning,
	Error
}

public interface INotificationService
{
	void Show(string title, string message, AppNotificationType type = AppNotificationType.Information);
}
