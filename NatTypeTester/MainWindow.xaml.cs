using NatTypeTester.ViewModels;
using ReactiveUI;
using STUN.Enums;
using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace NatTypeTester
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            ViewModel = new MainWindowViewModel();

            this.WhenActivated(d =>
            {
                #region Server

                this.Bind(ViewModel,
                        vm => vm.StunServer,
                        v => v.ServersComboBox.Text
                ).DisposeWith(d);

                this.OneWayBind(ViewModel,
                        vm => vm.StunServers,
                        v => v.ServersComboBox.ItemsSource
                ).DisposeWith(d);

                #endregion

                #region Proxy

                this.Bind(ViewModel,
                        vm => vm.ProxyServer,
                        v => v.ProxyServerTextBox.Text
                ).DisposeWith(d);

                this.Bind(ViewModel,
                        vm => vm.ProxyUser,
                        v => v.ProxyUsernameTextBox.Text
                ).DisposeWith(d);

                this.Bind(ViewModel,
                        vm => vm.ProxyPassword,
                        v => v.ProxyPasswordTextBox.Text
                ).DisposeWith(d);

                this.WhenAnyValue(x => x.ProxyTypeNoneRadio.IsChecked, x => x.ProxyTypeSocks5Radio.IsChecked)
                    .Subscribe(values =>
                    {
                        ProxyConfigGrid.IsEnabled = !values.Item1.GetValueOrDefault(false);
                        if (values.Item1.GetValueOrDefault(false))
                        {
                            ViewModel.ProxyType = ProxyType.Plain;
                        }
                        else if (values.Item2.GetValueOrDefault(false))
                        {
                            ViewModel.ProxyType = ProxyType.Socks5;
                        }
                    }).DisposeWith(d);

                #endregion

                #region RFC3489

                this.OneWayBind(ViewModel,
                        vm => vm.ClassicNatType,
                        v => v.NatTypeTextBox.Text
                ).DisposeWith(d);

                this.Bind(ViewModel,
                        vm => vm.LocalEnd,
                        v => v.LocalEndTextBox.Text
                ).DisposeWith(d);

                this.OneWayBind(ViewModel,
                        vm => vm.PublicEnd,
                        v => v.PublicEndTextBox.Text
                ).DisposeWith(d);

                this.BindCommand(ViewModel,
                                viewModel => viewModel.TestClassicNatType,
                                view => view.TestButton)
                        .DisposeWith(d);

                RFC3489Tab.Events().KeyDown
                        .Where(x => x.Key == Key.Enter && TestButton.IsEnabled)
                        .Subscribe(y => { TestButton.Command.Execute(Unit.Default); })
                        .DisposeWith(d);

                #endregion

                #region RFC5780

                this.OneWayBind(ViewModel,
                        vm => vm.BindingTest,
                        v => v.BindingTestTextBox.Text
                ).DisposeWith(d);

                this.OneWayBind(ViewModel,
                        vm => vm.MappingBehavior,
                        v => v.MappingBehaviorTextBox.Text
                ).DisposeWith(d);

                this.OneWayBind(ViewModel,
                        vm => vm.FilteringBehavior,
                        v => v.FilteringBehaviorTextBox.Text
                ).DisposeWith(d);

                this.Bind(ViewModel,
                        vm => vm.LocalAddress,
                        v => v.LocalAddressTextBox.Text
                ).DisposeWith(d);

                this.OneWayBind(ViewModel,
                        vm => vm.MappingAddress,
                        v => v.MappingAddressTextBox.Text
                ).DisposeWith(d);

                this.BindCommand(ViewModel,
                                viewModel => viewModel.DiscoveryNatType,
                                view => view.DiscoveryButton)
                        .DisposeWith(d);

                RFC5780Tab.Events().KeyDown
                        .Where(x => x.Key == Key.Enter && DiscoveryButton.IsEnabled)
                        .Subscribe(y => { DiscoveryButton.Command.Execute(Unit.Default); })
                        .DisposeWith(d);

                #endregion
            });
        }
    }
}
