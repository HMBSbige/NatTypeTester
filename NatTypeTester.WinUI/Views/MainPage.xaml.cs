namespace NatTypeTester.Views;

internal sealed partial class MainPage
{
	public MainPage()
	{
		InitializeComponent();
		ViewModel = Locator.Current.GetRequiredService<MainWindowViewModel>();

		IAbpLazyServiceProvider serviceProvider = Locator.Current.GetRequiredService<IAbpLazyServiceProvider>();

		this.WhenActivated(d =>
		{
			#region DPI

			double scale = XamlRoot.RasterizationScale;
			if (scale is not 1.0)
			{
				AppWindow appWindow = Locator.Current.GetRequiredService<MainWindow>().AppWindow;
				appWindow.Resize(new SizeInt32((int)(appWindow.Size.Width * scale), (int)(appWindow.Size.Height * scale)));
			}

			#endregion

			this.Bind(ViewModel,
				vm => vm.Config.StunServer,
				v => v.ServersComboBox.Text
			).DisposeWith(d);

			this.OneWayBind(ViewModel,
				vm => vm.StunServers,
				v => v.ServersComboBox.ItemsSource
			).DisposeWith(d);

			this.OneWayBind(ViewModel, vm => vm.Router, v => v.RoutedViewHost.Router).DisposeWith(d);

			NavigationView.Events().SelectionChanged.Subscribe(parameter =>
			{
				if (parameter.args.IsSettingsSelected)
				{
					ViewModel.Router.Navigate.Execute(serviceProvider.LazyGetRequiredService<SettingViewModel>()).Subscribe().Dispose();
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
						ViewModel.Router.Navigate.Execute(serviceProvider.LazyGetRequiredService<RFC5780ViewModel>()).Subscribe().Dispose();
						break;
					}
					case @"2":
					{
						ViewModel.Router.Navigate.Execute(serviceProvider.LazyGetRequiredService<RFC3489ViewModel>()).Subscribe().Dispose();
						break;
					}
				}
			}).DisposeWith(d);
			NavigationView.SelectedItem = NavigationView.MenuItems.OfType<NavigationViewItem>().First();

			ViewModel.LoadStunServer();
			ServersComboBox.SelectedIndex = 0;
		});
	}
}
