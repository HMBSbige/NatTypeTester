using System;
using System.Windows;

namespace NatTypeTester
{
	internal static class App
	{
		[STAThread]
		private static void Main()
		{
			var app = new Application();
			var win = new MainWindow();

			app.MainWindow = win;
			win.Show();

			app.ShutdownMode = ShutdownMode.OnMainWindowClose;
			app.Run();
		}
	}
}
