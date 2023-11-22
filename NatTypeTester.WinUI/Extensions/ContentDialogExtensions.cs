namespace NatTypeTester.Extensions;

internal static class ContentDialogExtensions
{
	public static async ValueTask HandleExceptionWithContentDialogAsync(this Exception ex, XamlRoot root)
	{
		ResourceLoader resourceLoader = ResourceLoader.GetForViewIndependentUse();
		ContentDialog dialog = new();
		try
		{
			dialog.XamlRoot = root;
			dialog.Title = nameof(NatTypeTester);

			string content = resourceLoader.GetString(ex.Message);
			if (string.IsNullOrEmpty(content))
			{
				content = ex.Message;
			}
			dialog.Content = content;

			dialog.PrimaryButtonText = resourceLoader.GetString(@"OK");

			await dialog.ShowAsync();
		}
		finally
		{
			dialog.Hide();
		}
	}
}
