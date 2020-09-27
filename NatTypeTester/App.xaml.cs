using ReactiveUI;
using Splat;
using System.Reflection;
using System.Windows;

namespace NatTypeTester
{
    public partial class App
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}