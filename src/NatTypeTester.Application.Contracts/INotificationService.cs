namespace NatTypeTester.Application.Contracts;

public interface INotificationService
{
	void Show(string title, string message, AppNotificationType type = AppNotificationType.Information);
}
