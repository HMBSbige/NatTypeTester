namespace NatTypeTester.Application;

public static class NatTypeTesterApplicationServiceCollectionExtensions
{
	public static IServiceCollection AddNatTypeTesterApplication(this IServiceCollection services)
	{
		services.AddNatTypeTesterApplicationContracts();
		services.AddNatTypeTesterDomain();

		services.AddHttpClient();
		services.TryAddTransient<IDnsClient, DefaultDnsClient>();
		services.TryAddKeyedTransient<IDnsClient, DefaultAClient>(AddressFamily.InterNetwork);
		services.TryAddKeyedTransient<IDnsClient, DefaultAAAAClient>(AddressFamily.InterNetworkV6);
		services.TryAddTransient<StunTestInputResolver>();
		services.TryAddTransient<IRfc3489AppService, Rfc3489AppService>();
		services.TryAddTransient<IRfc5780AppService, Rfc5780AppService>();
		services.TryAddTransient<IStunServerListAppService, StunServerListAppService>();
		services.TryAddTransient<IUpdateAppService, UpdateAppService>();

		return services;
	}
}
