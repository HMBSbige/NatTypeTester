using JetBrains.Annotations;
using NatTypeTester.Utils;
using NatTypeTester.ViewModels;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using STUN.Enums;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.Views;

[ExposeServices(typeof(IViewFor<RFC5780ViewModel>))]
[UsedImplicitly]
public partial class RFC5780View : ITransientDependency
{
	public RFC5780View(RFC5780ViewModel viewModel)
	{
		InitializeComponent();
		ViewModel = viewModel;

		this.WhenActivated(d =>
		{
			this.Bind(ViewModel, vm => vm.TransportType, v => v.TransportTypeRadioButtons.SelectedIndex, type => (int)type, index => (TransportType)index).DisposeWith(d);
			ViewModel.WhenAnyValue(vm => vm.TransportType).Subscribe(_ => ViewModel.ResetResult()).DisposeWith(d);
			this.OneWayBind(ViewModel, vm => vm.TransportType, v => v.FilteringBehaviorTextBox.Visibility, type => type is TransportType.Udp ? Visibility.Visible : Visibility.Collapsed).DisposeWith(d);

			this.OneWayBind(ViewModel, vm => vm.Result5389.BindingTestResult, v => v.BindingTestTextBox.Text).DisposeWith(d);

			this.OneWayBind(ViewModel, vm => vm.Result5389.MappingBehavior, v => v.MappingBehaviorTextBox.Text).DisposeWith(d);

			this.OneWayBind(ViewModel, vm => vm.Result5389.FilteringBehavior, v => v.FilteringBehaviorTextBox.Text).DisposeWith(d);

			this.Bind(ViewModel, vm => vm.Result5389.LocalEndPoint, v => v.LocalAddressComboBox.Text).DisposeWith(d);

			LocalAddressComboBox.Events().LostKeyboardFocus.Subscribe(_ => LocalAddressComboBox.Text = ViewModel.Result5389.LocalEndPoint?.ToString() ?? string.Empty).DisposeWith(d);

			this.OneWayBind(ViewModel, vm => vm.Result5389.PublicEndPoint, v => v.MappingAddressTextBox.Text).DisposeWith(d);

			this.BindCommand(ViewModel, vm => vm.DiscoveryNatType, v => v.DiscoveryButton).DisposeWith(d);

			this.Events().KeyDown
				.Where(x => x.Key is Key.Enter && DiscoveryButton.Command.CanExecute(default))
				.Subscribe(_ => DiscoveryButton.Command.Execute(default))
				.DisposeWith(d);

			ViewModel.DiscoveryNatType.ThrownExceptions.Subscribe(ex => _ = ex.HandleExceptionWithContentDialogAsync()).DisposeWith(d);

			ViewModel.DiscoveryNatType.IsExecuting.Subscribe(b => TransportTypeRadioButtons.IsEnabled = !b).DisposeWith(d);
		});
	}
}
