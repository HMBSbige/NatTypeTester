using ModernWpf.Controls;

namespace NatTypeTester.Dialogs;

public class DisposableContentDialog : ContentDialog, IDisposable
{
	public void Dispose()
	{
		Hide();
	}
}
