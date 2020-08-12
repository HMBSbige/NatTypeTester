using System.Reactive.Disposables;
using NatTypeTester.ViewModels;
using ReactiveUI;

namespace NatTypeTester
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel();

            this.WhenActivated(disposableRegistration =>
            {
                this.Bind(ViewModel,
                        vm => vm.StunServer,
                        v => v.ServersComboBox.Text
                ).DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        vm => vm.StunServers,
                        v => v.ServersComboBox.ItemsSource
                ).DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        vm => vm.ClassicNatType,
                        v => v.NatTypeTextBox.Text
                ).DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                        vm => vm.LocalEnd,
                        v => v.LocalEndTextBox.Text
                ).DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        vm => vm.PublicEnd,
                        v => v.PublicEndTextBox.Text
                ).DisposeWith(disposableRegistration);

                this.BindCommand(ViewModel,
                                viewModel => viewModel.TestClassicNatType,
                                view => view.TestButton)
                        .DisposeWith(disposableRegistration);
            });
        }
    }
}
