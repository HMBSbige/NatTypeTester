namespace NatTypeTester.Domain.Shared.Configuration;

public static class ConfigurationConsts
{
	private const string ConfigFileName = "config.json";

	public static string ConfigDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(NatTypeTester));

	public static string ConfigFilePath => Path.Combine(ConfigDirectory, ConfigFileName);

	public static readonly ImmutableArray<string> DefaultStunServers =
	[
		"stun.fitauto.ru",
		"stun.m-online.net",
		"stun.mixvoip.com",
		"stun.voipgate.com",
		"stun.t-online.de",
		"stun.srce.hr",
		"stun.aa.net.uk",
		"stun.miwifi.com"
	];
}
