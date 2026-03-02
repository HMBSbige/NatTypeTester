namespace NatTypeTester.Views.Views;

public partial class SettingsView : ReactiveUserControl<SettingsViewModel>, ISingletonDependency
{
	public SettingsView()
	{
		InitializeComponent();

		this.WhenActivated
		(d =>
			{
				UpdateLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("Update")).DisposeWith(d);
				CheckUpdateButtonText.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("CheckUpdate")).DisposeWith(d);
				OpenHomepageButtonText.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("OpenHomepage")).DisposeWith(d);
				AutoCheckUpdateCheckBox.Bind(ContentProperty, new ObservableStringLocalizer("AutoCheckUpdate")).DisposeWith(d);
				CheckUpdateIntervalLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("CheckUpdateInterval")).DisposeWith(d);
				IncludePreReleaseCheckBox.Bind(ContentProperty, new ObservableStringLocalizer("IncludePreRelease")).DisposeWith(d);
				ProxyLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("Proxy")).DisposeWith(d);
				NoProxyRadioButton.Bind(ContentProperty, new ObservableStringLocalizer("NoProxy")).DisposeWith(d);
				Socks5ProxyRadioButton.Bind(ContentProperty, new ObservableStringLocalizer("SOCKS5Proxy")).DisposeWith(d);
				ProxyServerLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("ProxyServer")).DisposeWith(d);
				ProxyUsernameLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("ProxyUsername")).DisposeWith(d);
				ProxyPasswordLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("ProxyPassword")).DisposeWith(d);
				LanguageLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("Language")).DisposeWith(d);
				StunServerListLabel.Bind(TextBlock.TextProperty, new ObservableStringLocalizer("StunServerList")).DisposeWith(d);
				LoadStunServerListButton.Bind(ToolTip.TipProperty, new ObservableStringLocalizer("LoadStunServerList")).DisposeWith(d);
			}
		);
	}
}
