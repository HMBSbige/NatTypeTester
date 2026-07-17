namespace NatTypeTester.Configuration;

public static class NatTypeTesterConfigurationServiceCollectionExtensions
{
	public static IServiceCollection AddNatTypeTesterConfiguration(this IServiceCollection services)
	{
		services.AddNatTypeTesterDomain();

		services.AddOptions<AppConfigStorageOptions>();
		services.AddSingleton<IAppConfigManager, AppConfigManager>();

		return services;
	}
}
