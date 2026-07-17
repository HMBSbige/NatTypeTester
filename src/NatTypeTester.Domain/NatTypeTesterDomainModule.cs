namespace NatTypeTester.Domain;

public static class NatTypeTesterDomainServiceCollectionExtensions
{
	public static IServiceCollection AddNatTypeTesterDomain(this IServiceCollection services)
	{
		services.AddNatTypeTesterDomainShared();

		return services;
	}
}
