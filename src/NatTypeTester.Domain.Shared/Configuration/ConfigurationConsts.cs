namespace NatTypeTester.Domain.Shared.Configuration;

public static class ConfigurationConsts
{
	private const string ConfigFileName = "config.json";

	public static string ConfigDirectory => Path.Combine
	(
		Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
		nameof(NatTypeTester)
	);

	public static string ConfigFilePath => Path.Combine(ConfigDirectory, ConfigFileName);

	public static readonly ImmutableArray<string> DefaultStunServers =
	[
		"stun.hot-chilli.net",
		"stun.fitauto.ru",
		"stun.internetcalls.com",
		"stun.voip.aebc.com",
		"stun.voipbuster.com",
		"stun.voipstunt.com"
	];
}
