using NatTypeTester.Utils;
using NatTypeTester.ViewModels;
using ReactiveUI;
using STUN.Utils;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Controls;
using System.Windows.Input;

namespace NatTypeTester.Views
{
	public partial class RFC3489View
	{
		public RFC3489View(RFC3489ViewModel viewModel)
		{
			InitializeComponent();
			ViewModel = viewModel;

			this.WhenActivated(d =>
			{
				this.OneWayBind(ViewModel,
								vm => vm.Result3489.NatType,
								v => v.NatTypeTextBox.Text,
								type => type.ToString()
						)
						.DisposeWith(d);

				this.Bind(ViewModel,
								vm => vm.Result3489.LocalEndPoint,
								v => v.LocalEndTextBox.Text,
								ipe => ipe is null ? string.Empty : ipe.ToString(),
								NetUtils.ParseEndpoint
						)
						.DisposeWith(d);

				this.OneWayBind(ViewModel,
								vm => vm.Result3489.PublicEndPoint,
								v => v.PublicEndTextBox.Text,
								ipe => ipe is null ? string.Empty : ipe.ToString()
						)
						.DisposeWith(d);

				this.BindCommand(ViewModel, vm => vm.TestClassicNatType, v => v.TestButton).DisposeWith(d);

				this.Events()
						.KeyDown
						.Where(x => x.Key == Key.Enter && TestButton.Command.CanExecute(default))
						.Subscribe(_ => TestButton.Command.Execute(default))
						.DisposeWith(d);

				ViewModel.TestClassicNatType.ThrownExceptions.Subscribe(async ex => await ex.HandleExceptionWithContentDialogAsync()).DisposeWith(d);
			});
		}
	}
}
