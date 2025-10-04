using JetBrains.Annotations;
using NatTypeTester.Utils;
using NatTypeTester.ViewModels;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Windows.Input;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.Views;

[ExposeServices(typeof(IViewFor<RFC3489ViewModel>))]
[UsedImplicitly]
public partial class RFC3489View : ITransientDependency
{
	public RFC3489View(RFC3489ViewModel viewModel)
	{
		InitializeComponent();
		ViewModel = viewModel;

		this.WhenActivated(d =>
		{
			this.OneWayBind(ViewModel, vm => vm.Result3489.NatType, v => v.NatTypeTextBox.Text).DisposeWith(d);

			this.Bind(ViewModel, vm => vm.Result3489.LocalEndPoint, v => v.LocalEndComboBox.Text).DisposeWith(d);

			LocalEndComboBox.Events().LostKeyboardFocus.Subscribe(_ => LocalEndComboBox.Text = ViewModel.Result3489.LocalEndPoint?.ToString() ?? string.Empty).DisposeWith(d);

			this.OneWayBind(ViewModel, vm => vm.Result3489.PublicEndPoint, v => v.PublicEndTextBox.Text).DisposeWith(d);

			this.BindCommand(ViewModel, vm => vm.TestClassicNatType, v => v.TestButton).DisposeWith(d);

			this.Events().KeyDown
				.Where(x => x.Key == Key.Enter && TestButton.Command.CanExecute(default))
				.Subscribe(_ => TestButton.Command.Execute(default))
				.DisposeWith(d);

			ViewModel.TestClassicNatType.ThrownExceptions.Subscribe(ex => _ = ex.HandleExceptionWithContentDialogAsync()).DisposeWith(d);
		});
	}
}
