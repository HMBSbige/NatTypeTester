namespace NatTypeTester.Domain.Shared.Configuration;

public static class ConfigurationConsts
{
	public const string ConfigFileName = "config.json";

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
