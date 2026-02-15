namespace NatTypeTester.Views.Views;

public partial class RFC3489View : ReactiveUserControl<RFC3489ViewModel>, ISingletonDependency
{
	public RFC3489View()
	{
		InitializeComponent();

		this.WhenActivated
		(d =>
			{
				Rfc3489WarningText.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("RFC3489Warning")).DisposeWith(d);
				NatTypeLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("NatType")).DisposeWith(d);
				LocalEndLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("LocalEnd")).DisposeWith(d);
				LocalEndComboBox.Bind(ComboBox.PlaceholderTextProperty, new ObservableStringLocalizer("LocalEndPlaceholder")).DisposeWith(d);
				PublicEndLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("PublicEnd")).DisposeWith(d);
				TestButton.Bind(ContentProperty, new ObservableStringLocalizer("Test")).DisposeWith(d);
			}
		);
	}
}
