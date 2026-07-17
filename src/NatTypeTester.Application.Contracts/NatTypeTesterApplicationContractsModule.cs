namespace NatTypeTester.Application.Contracts;

public static class NatTypeTesterApplicationContractsServiceCollectionExtensions
{
	public static IServiceCollection AddNatTypeTesterApplicationContracts(this IServiceCollection services)
	{
		services.AddNatTypeTesterDomainShared();

		return services;
	}
}
