using ModernWpf.Controls;
using System;

namespace NatTypeTester.Dialogs
{
	public class DisposableContentDialog : ContentDialog, IDisposable
	{
		public void Dispose()
		{
			Hide();
		}
	}
}
