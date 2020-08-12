using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
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
                #region Server

                this.Bind(ViewModel,
                        vm => vm.StunServer,
                        v => v.ServersComboBox.Text
                ).DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        vm => vm.StunServers,
                        v => v.ServersComboBox.ItemsSource
                ).DisposeWith(disposableRegistration);

                #endregion

                #region RFC3489

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

                RFC3489Tab.Events().KeyDown
                        .Where(x => x.Key == Key.Enter && TestButton.IsEnabled)
                        .Subscribe(y => { TestButton.Command.Execute(Unit.Default); })
                        .DisposeWith(disposableRegistration);

                #endregion

                #region RFC5780

                this.OneWayBind(ViewModel,
                        vm => vm.BindingTest,
                        v => v.BindingTestTextBox.Text
                ).DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        vm => vm.MappingBehavior,
                        v => v.MappingBehaviorTextBox.Text
                ).DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        vm => vm.FilteringBehavior,
                        v => v.FilteringBehaviorTextBox.Text
                ).DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                        vm => vm.LocalAddress,
                        v => v.LocalAddressTextBox.Text
                ).DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        vm => vm.MappingAddress,
                        v => v.MappingAddressTextBox.Text
                ).DisposeWith(disposableRegistration);

                this.BindCommand(ViewModel,
                                viewModel => viewModel.DiscoveryNatType,
                                view => view.DiscoveryButton)
                        .DisposeWith(disposableRegistration);

                RFC5780Tab.Events().KeyDown
                        .Where(x => x.Key == Key.Enter && DiscoveryButton.IsEnabled)
                        .Subscribe(y => { DiscoveryButton.Command.Execute(Unit.Default); })
                        .DisposeWith(disposableRegistration);

                #endregion
            });
        }
    }
}
