using ModernWpf;
using NatTypeTester.ViewModels;
using ReactiveUI;
using STUN.Enums;
using STUN.Utils;
using System;
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
			ThemeManager.Current.ApplicationTheme = null;

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
						vm => vm.Result3489.NatType,
						v => v.NatTypeTextBox.Text,
						type => type.ToString()
				).DisposeWith(d);

				this.Bind(ViewModel,
						vm => vm.Result3489.LocalEndPoint,
						v => v.LocalEndTextBox.Text,
						ipe => ipe is null ? string.Empty : ipe.ToString(),
						NetUtils.ParseEndpoint
				).DisposeWith(d);

				this.OneWayBind(ViewModel,
						vm => vm.Result3489.PublicEndPoint,
						v => v.PublicEndTextBox.Text,
						ipe => ipe is null ? string.Empty : ipe.ToString()
				).DisposeWith(d);

				this.BindCommand(ViewModel, viewModel => viewModel.TestClassicNatType, view => view.TestButton).DisposeWith(d);

				RFC3489Tab.Events().KeyDown
						.Where(x => x.Key == Key.Enter && TestButton.IsEnabled)
						.Subscribe(async _ => await ViewModel.TestClassicNatType.Execute(default))
						.DisposeWith(d);

				#endregion

				#region RFC5780

				this.OneWayBind(ViewModel,
						vm => vm.Result5389.BindingTestResult,
						v => v.BindingTestTextBox.Text,
						res => res.ToString()
				).DisposeWith(d);

				this.OneWayBind(ViewModel,
						vm => vm.Result5389.MappingBehavior,
						v => v.MappingBehaviorTextBox.Text,
						res => res.ToString()
				).DisposeWith(d);

				this.OneWayBind(ViewModel,
						vm => vm.Result5389.FilteringBehavior,
						v => v.FilteringBehaviorTextBox.Text,
						res => res.ToString()
				).DisposeWith(d);

				this.Bind(ViewModel,
						vm => vm.Result5389.LocalEndPoint,
						v => v.LocalAddressTextBox.Text,
						ipe => ipe is null ? string.Empty : ipe.ToString(),
						NetUtils.ParseEndpoint
				).DisposeWith(d);

				this.OneWayBind(ViewModel,
						vm => vm.Result5389.PublicEndPoint,
						v => v.MappingAddressTextBox.Text,
						ipe => ipe is null ? string.Empty : ipe.ToString()
				).DisposeWith(d);

				this.BindCommand(ViewModel, viewModel => viewModel.DiscoveryNatType, view => view.DiscoveryButton).DisposeWith(d);

				RFC5780Tab.Events().KeyDown
						.Where(x => x.Key == Key.Enter && DiscoveryButton.IsEnabled)
						.Subscribe(async _ => await ViewModel.DiscoveryNatType.Execute(default))
						.DisposeWith(d);

				#endregion
			});
		}
	}
}
