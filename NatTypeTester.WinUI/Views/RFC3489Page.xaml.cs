namespace NatTypeTester.Views;

[ExposeServices(typeof(IViewFor<RFC3489ViewModel>))]
[UsedImplicitly]
internal sealed partial class RFC3489Page : ITransientDependency
{
	public RFC3489Page(RFC3489ViewModel viewModel)
	{
		InitializeComponent();
		ViewModel = viewModel;

		this.WhenActivated(d =>
		{
			this.OneWayBind(ViewModel, vm => vm.Result3489.NatType, v => v.NatTypeTextBox.Text).DisposeWith(d);

			this.Bind(ViewModel, vm => vm.Result3489.LocalEndPoint, v => v.LocalEndComboBox.Text).DisposeWith(d);

			LocalEndComboBox.Events().LostFocus.Subscribe(_ => LocalEndComboBox.Text = ViewModel.Result3489.LocalEndPoint?.ToString()).DisposeWith(d);

			this.OneWayBind(ViewModel, vm => vm.Result3489.PublicEndPoint, v => v.PublicEndTextBox.Text).DisposeWith(d);

			this.BindCommand(ViewModel, vm => vm.TestClassicNatType, v => v.TestButton).DisposeWith(d);

			this.Events().KeyDown
				.Where(x => x.Key is VirtualKey.Enter && TestButton.Command.CanExecute(default))
				.Subscribe(_ => TestButton.Command.Execute(default))
				.DisposeWith(d);

			ViewModel.TestClassicNatType.ThrownExceptions.Subscribe(ex => ex.HandleExceptionWithContentDialogAsync(Content.XamlRoot).Forget()).DisposeWith(d);
		});
	}
}
