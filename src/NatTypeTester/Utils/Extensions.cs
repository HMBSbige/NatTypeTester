using NatTypeTester.Dialogs;

namespace NatTypeTester.Utils;

public static class Extensions
{
	public static async Task HandleExceptionWithContentDialogAsync(this Exception ex)
	{
		using DisposableContentDialog dialog = new();
		dialog.Title = nameof(NatTypeTester);
		dialog.Content = ex.Message;
		dialog.PrimaryButtonText = @"OK";
		await dialog.ShowAsync();
	}
}
