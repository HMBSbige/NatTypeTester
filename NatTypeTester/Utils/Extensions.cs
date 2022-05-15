using NatTypeTester.Dialogs;

namespace NatTypeTester.Utils;

public static class Extensions
{
	public static async Task HandleExceptionWithContentDialogAsync(this Exception ex)
	{
		using DisposableContentDialog dialog = new()
		{
			Title = nameof(NatTypeTester),
			Content = ex.Message,
			PrimaryButtonText = @"OK"
		};
		await dialog.ShowAsync();
	}
}
