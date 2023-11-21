namespace NatTypeTester.Extensions;

internal static class ContentDialogExtensions
{
	public static async ValueTask HandleExceptionWithContentDialogAsync(this Exception ex, XamlRoot root)
	{
		ContentDialog dialog = new();
		try
		{
			dialog.XamlRoot = root;
			dialog.Title = nameof(NatTypeTester);
			dialog.Content = ex.Message;
			dialog.PrimaryButtonText = @"OK";

			await dialog.ShowAsync();
		}
		finally
		{
			dialog.Hide();
		}
	}
}
