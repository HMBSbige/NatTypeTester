using NatTypeTester.ViewModels;
using ReactiveUI;
using STUN.Enums;
using System.Reactive.Disposables;

namespace NatTypeTester.Views
{
	public partial class SettingView
	{
		public SettingView(SettingViewModel viewModel)
		{
			InitializeComponent();
			ViewModel = viewModel;

			this.WhenActivated(d =>
			{
				this.Bind(ViewModel, vm => vm.Config.ProxyServer, v => v.ProxyServerTextBox.Text).DisposeWith(d);

				this.Bind(ViewModel, vm => vm.Config.ProxyUser, v => v.ProxyUsernameTextBox.Text).DisposeWith(d);

				this.Bind(ViewModel, vm => vm.Config.ProxyPassword, v => v.ProxyPasswordTextBox.Text).DisposeWith(d);

				this.Bind(ViewModel,
					vm => vm.Config.ProxyType,
					v => v.ProxyRadioButtons.SelectedIndex,
					type => (int)type,
					index =>
					{
						var type = (ProxyType)index;
						ProxyConfigGrid.IsEnabled = type is not ProxyType.Plain;
						return type;
					}).DisposeWith(d);
			});
		}
	}
}
