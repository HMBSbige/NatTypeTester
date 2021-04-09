using NatTypeTester.Services;
using System.Windows;

namespace NatTypeTester
{
	public partial class App
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			DI.Register();

			MainWindow = DI.GetRequiredService<MainWindow>();
			MainWindow.Show();
		}
	}
}
