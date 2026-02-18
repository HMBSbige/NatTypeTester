namespace NatTypeTester.Views.Views;

public partial class MainView : ReactiveUserControl<MainWindowViewModel>, ISingletonDependency
{
	public MainView()
	{
		InitializeComponent();

		this.WhenActivated
		(d =>
			{
				StunServerLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("StunServer")).DisposeWith(d);
				AddStunServerButton.Bind(ToolTip.TipProperty, new ObservableStringLocalizer("AddStunServer")).DisposeWith(d);
				DeleteStunServerButton.Bind(ToolTip.TipProperty, new ObservableStringLocalizer("DeleteStunServer")).DisposeWith(d);
				Rfc5780TabLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("RFC5780")).DisposeWith(d);
				Rfc3489TabLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("RFC3489")).DisposeWith(d);
				SettingsTabLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("Settings")).DisposeWith(d);
			}
		);
	}
}
