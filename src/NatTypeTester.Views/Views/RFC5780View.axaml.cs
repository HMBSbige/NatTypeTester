namespace NatTypeTester.Views.Views;

public partial class RFC5780View : ReactiveUserControl<RFC5780ViewModel>, ISingletonDependency
{
	public RFC5780View()
	{
		InitializeComponent();

		this.WhenActivated
		(d =>
			{
				BindingTestLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("BindingTest")).DisposeWith(d);
				MappingBehaviorLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("MappingBehavior")).DisposeWith(d);
				FilteringBehaviorLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("FilteringBehavior")).DisposeWith(d);
				LocalEndLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("LocalEnd")).DisposeWith(d);
				LocalEndComboBox.Bind(ComboBox.PlaceholderTextProperty, new ObservableStringLocalizer("LocalEndPlaceholder")).DisposeWith(d);
				PublicEndLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("PublicEnd")).DisposeWith(d);
				TestButton.Bind(ContentProperty, new ObservableStringLocalizer("Test")).DisposeWith(d);
			}
		);
	}
}
