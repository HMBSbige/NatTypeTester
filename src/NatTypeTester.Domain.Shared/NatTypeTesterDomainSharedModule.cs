namespace NatTypeTester.Domain.Shared;

public static class NatTypeTesterDomainSharedServiceCollectionExtensions
{
	public static IServiceCollection AddNatTypeTesterDomainShared(this IServiceCollection services)
	{
		services.TryAddSingleton(TimeProvider.System);

		return services;
	}
}
