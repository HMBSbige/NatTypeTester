namespace NatTypeTester.Views.Views;

public partial class MainView : ReactiveUserControl<MainWindowViewModel>, ISingletonDependency
{
	public MainView()
	{
		InitializeComponent();

		this.WhenActivated
		(d =>
			{
				Rfc5780NavLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("RFC5780")).DisposeWith(d);
				Rfc3489NavLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("RFC3489")).DisposeWith(d);
				SettingsNavLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("Settings")).DisposeWith(d);
			}
		);
	}
}
