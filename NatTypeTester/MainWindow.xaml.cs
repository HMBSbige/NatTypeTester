using ModernWpf;
using ModernWpf.Controls;
using NatTypeTester.ViewModels;
using ReactiveUI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace NatTypeTester
{
	public partial class MainWindow
	{
		public MainWindow(MainWindowViewModel viewModel,
			RFC5780ViewModel rfc5780ViewModel,
			RFC3489ViewModel rfc3489ViewModel,
			SettingViewModel settingViewModel
			)
		{
			InitializeComponent();
			ViewModel = viewModel;
			ThemeManager.Current.ApplicationTheme = null;

			this.WhenActivated(d =>
			{
				#region Server

				this.Bind(ViewModel,
						vm => vm.Config.StunServer,
						v => v.ServersComboBox.Text
				).DisposeWith(d);

				this.OneWayBind(ViewModel,
						vm => vm.StunServers,
						v => v.ServersComboBox.ItemsSource
				).DisposeWith(d);

				#endregion

				this.Bind(ViewModel, vm => vm.Router, v => v.RoutedViewHost.Router).DisposeWith(d);
				Observable.FromEventPattern<NavigationViewSelectionChangedEventArgs>(NavigationView, nameof(NavigationView.SelectionChanged))
				.Subscribe(args =>
				{
					if (args.EventArgs.IsSettingsSelected)
					{
						ViewModel.Router.Navigate.Execute(settingViewModel);
						return;
					}

					if (args.EventArgs.SelectedItem is not NavigationViewItem { Tag: string tag })
					{
						return;
					}

					switch (tag)
					{
						case @"1":
						{
							ViewModel.Router.Navigate.Execute(rfc5780ViewModel);
							break;
						}
						case @"2":
						{
							ViewModel.Router.Navigate.Execute(rfc3489ViewModel);
							break;
						}
					}
				}).DisposeWith(d);
				NavigationView.SelectedItem = NavigationView.MenuItems.OfType<NavigationViewItem>().First();
			});
		}
	}
}
