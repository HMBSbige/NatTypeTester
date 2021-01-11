using NatTypeTester.Dialogs;
using System;
using System.Threading.Tasks;

namespace NatTypeTester.Utils
{
	public static class Extensions
	{
		public static async Task HandleExceptionWithContentDialogAsync(this Exception ex)
		{
			using var dialog = new DisposableContentDialog
			{
				Title = nameof(NatTypeTester),
				Content = ex.Message,
				PrimaryButtonText = @"OK"
			};
			await dialog.ShowAsync();
		}
	}
}
