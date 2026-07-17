namespace NatTypeTester.ViewModels;

public sealed partial class ConnectionSettingsViewModel : ViewModelBase
{
	private bool _isInitialized;

	[Reactive]
	public partial ProxyType ProxyType { get; set; }

	[Reactive]
	public partial string? ProxyServer { get; set; }

	[Reactive]
	public partial string? ProxyUser { get; set; }

	[Reactive]
	public partial string? ProxyPassword { get; set; }

	[Reactive]
	public partial bool SkipCertificateValidation { get; set; }

	internal void ApplyConfig(AppConfig config)
	{
		if (_isInitialized)
		{
			return;
		}

		ProxyType = config.ProxyType;
		ProxyServer = config.ProxyServer;
		ProxyUser = config.ProxyUser;
		ProxyPassword = config.ProxyPassword;
		SkipCertificateValidation = config.SkipCertificateValidation;

		PersistToConfig
		(
			this.WhenAnyValue
			(
				static viewModel => viewModel.ProxyType,
				static viewModel => viewModel.ProxyServer,
				static viewModel => viewModel.ProxyUser,
				static viewModel => viewModel.ProxyPassword,
				static viewModel => viewModel.SkipCertificateValidation,
				static (proxyType, proxyServer, proxyUser, proxyPassword, skipCertificateValidation) =>
					new ConnectionConfigSnapshot(proxyType, proxyServer, proxyUser, proxyPassword, skipCertificateValidation)
			),
			static (appConfig, value) =>
			{
				appConfig.ProxyType = value.ProxyType;
				appConfig.ProxyServer = value.ProxyServer;
				appConfig.ProxyUser = value.ProxyUser;
				appConfig.ProxyPassword = value.ProxyPassword;
				appConfig.SkipCertificateValidation = value.SkipCertificateValidation;
			}
		);

		_isInitialized = true;
	}

	internal ProxyOptions CreateProxyOptions()
	{
		return new ProxyOptions
		{
			Type = ProxyType,
			Server = ProxyServer,
			UserName = ProxyUser,
			Password = ProxyPassword
		};
	}

	private readonly record struct ConnectionConfigSnapshot
	(
		ProxyType ProxyType,
		string? ProxyServer,
		string? ProxyUser,
		string? ProxyPassword,
		bool SkipCertificateValidation
	);
}
