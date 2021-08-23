using JetBrains.Annotations;
using NatTypeTester.Utils;
using NatTypeTester.ViewModels;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Input;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.Views
{
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
				this.OneWayBind(ViewModel, vm => vm.Result5389.BindingTestResult, v => v.BindingTestTextBox.Text).DisposeWith(d);

				this.OneWayBind(ViewModel, vm => vm.Result5389.MappingBehavior, v => v.MappingBehaviorTextBox.Text).DisposeWith(d);

				this.OneWayBind(ViewModel, vm => vm.Result5389.FilteringBehavior, v => v.FilteringBehaviorTextBox.Text).DisposeWith(d);

				this.Bind(ViewModel, vm => vm.Result5389.LocalEndPoint, v => v.LocalAddressTextBox.Text).DisposeWith(d);

				this.OneWayBind(ViewModel, vm => vm.Result5389.PublicEndPoint, v => v.MappingAddressTextBox.Text).DisposeWith(d);

				this.BindCommand(ViewModel, vm => vm.DiscoveryNatType, v => v.DiscoveryButton).DisposeWith(d);

				this.Events().KeyDown
						.Where(x => x.Key == Key.Enter && DiscoveryButton.Command.CanExecute(default))
						.Subscribe(_ => DiscoveryButton.Command.Execute(default))
						.DisposeWith(d);

				ViewModel.DiscoveryNatType.ThrownExceptions.Subscribe(ex => _ = ex.HandleExceptionWithContentDialogAsync()).DisposeWith(d);
			});
		}
	}
}
