namespace NatTypeTester.Views.Views;

public partial class SettingsView : ReactiveUserControl<SettingsViewModel>, ISingletonDependency
{
	public SettingsView()
	{
		InitializeComponent();

		this.WhenActivated
		(d =>
			{
				ProxyLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("Proxy")).DisposeWith(d);
				NoProxyRadioButton.Bind(ContentProperty, new ObservableStringLocalizer("NoProxy")).DisposeWith(d);
				Socks5ProxyRadioButton.Bind(ContentProperty, new ObservableStringLocalizer("SOCKS5Proxy")).DisposeWith(d);
				ProxyServerLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("ProxyServer")).DisposeWith(d);
				ProxyUsernameLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("ProxyUsername")).DisposeWith(d);
				ProxyPasswordLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("ProxyPassword")).DisposeWith(d);
				LanguageLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("Language")).DisposeWith(d);
			}
		);
	}
}
