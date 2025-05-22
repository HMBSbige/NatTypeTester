using Microsoft.Extensions.DependencyInjection;
using ModernWpf.Controls;
using NatTypeTester.ViewModels;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive.Disposables;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester;

public partial class MainWindow : ISingletonDependency
{
	public MainWindow(MainWindowViewModel viewModel, IServiceProvider serviceProvider)
	{
		InitializeComponent();
		ViewModel = viewModel;

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

			this.OneWayBind(ViewModel, vm => vm.Router, v => v.RoutedViewHost.Router).DisposeWith(d);

			NavigationView.Events().SelectionChanged
				.Subscribe(parameter =>
				{
					if (parameter.args.IsSettingsSelected)
					{
						ViewModel.Router.Navigate.Execute(serviceProvider.GetRequiredService<SettingViewModel>()).Subscribe().Dispose();
						return;
					}

					if (parameter.args.SelectedItem is not NavigationViewItem { Tag: string tag })
					{
						return;
					}

					switch (tag)
					{
						case @"1":
						{
							ViewModel.Router.Navigate.Execute(serviceProvider.GetRequiredService<RFC5780ViewModel>()).Subscribe().Dispose();
							break;
						}
						case @"2":
						{
							ViewModel.Router.Navigate.Execute(serviceProvider.GetRequiredService<RFC3489ViewModel>()).Subscribe().Dispose();
							break;
						}
					}
				}).DisposeWith(d);
			NavigationView.SelectedItem = NavigationView.MenuItems.OfType<NavigationViewItem>().First();

			ViewModel.LoadStunServer();
		});
	}
}
