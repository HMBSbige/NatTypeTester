using System;
using System.Reflection;
using System.Windows;
using ReactiveUI;
using Splat;

namespace NatTypeTester
{
    internal static class App
    {
        [STAThread]
        private static void Main()
        {
            var app = new Application();
            var win = new MainWindow();

            Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetCallingAssembly());

            app.MainWindow = win;
            win.Show();

            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            app.Run();
        }
    }
}
