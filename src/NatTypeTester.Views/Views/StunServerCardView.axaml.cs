namespace NatTypeTester.Views.Views;

public partial class StunServerCardView : ReactiveUserControl<MainWindowViewModel>, ISingletonDependency
{
	public StunServerCardView()
	{
		InitializeComponent();

		this.WhenActivated
		(d =>
			{
				StunServerLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("StunServer")).DisposeWith(d);
				AddStunServerButton.Bind(ToolTip.TipProperty, new ObservableStringLocalizer("AddStunServer")).DisposeWith(d);
				DeleteStunServerButton.Bind(ToolTip.TipProperty, new ObservableStringLocalizer("DeleteStunServer")).DisposeWith(d);
			}
		);
	}
}
