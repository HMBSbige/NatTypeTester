namespace NatTypeTester.ViewModels;

public static class NatTypeTesterViewModelsServiceCollectionExtensions
{
	public static IServiceCollection AddNatTypeTesterViewModels(this IServiceCollection services)
	{
		services.AddNatTypeTesterApplicationContracts();
		services.AddNatTypeTesterDomain();

		services.TryAddSingleton<ApplicationSettingsViewModel>();
		services.TryAddSingleton<ConnectionSettingsViewModel>();
		services.TryAddSingleton<StunServerSettingsViewModel>();
		services.TryAddSingleton<UpdateSettingsViewModel>();
		services.TryAddSingleton<MainWindowViewModel>();
		services.TryAddSingleton<RFC3489ViewModel>();
		services.TryAddSingleton<RFC5780ViewModel>();
		services.TryAddSingleton<SettingsViewModel>();

		return services;
	}
}
