using JetBrains.Annotations;
using NatTypeTester.ViewModels;
using ReactiveUI;
using STUN.Enums;
using System.Reactive.Disposables;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.Views;

[ExposeServices(typeof(IViewFor<SettingViewModel>))]
[UsedImplicitly]
public partial class SettingView : ITransientDependency
{
	public SettingView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			this.Bind(ViewModel, vm => vm.Config.ProxyServer, v => v.ProxyServerTextBox.Text).DisposeWith(d);

			this.Bind(ViewModel, vm => vm.Config.ProxyUser, v => v.ProxyUsernameTextBox.Text).DisposeWith(d);

			this.Bind(ViewModel, vm => vm.Config.ProxyPassword, v => v.ProxyPasswordTextBox.Text).DisposeWith(d);

			this.Bind(ViewModel, vm => vm.Config.ProxyType, v => v.ProxyRadioButtons.SelectedIndex, type => (int)type, index => (ProxyType)index).DisposeWith(d);

			this.OneWayBind(ViewModel, vm => vm.Config.ProxyType, v => v.ProxyConfigGrid.IsEnabled, type => type is not ProxyType.Plain).DisposeWith(d);
		});
	}
}
