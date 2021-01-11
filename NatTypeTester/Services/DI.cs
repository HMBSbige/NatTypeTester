using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;

namespace NatTypeTester.Services
{
	public static class DI
	{
		public static T GetService<T>()
		{
			return Locator.Current.GetService<T>();
		}

		public static void Register()
		{
			var services = new ServiceCollection();

			services.UseMicrosoftDependencyResolver();
			Locator.CurrentMutable.InitializeSplat();
			Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.Wpf);

			ConfigureServices(services);
		}

		private static IServiceCollection ConfigureServices(IServiceCollection services)
		{
			return services
				.AddViewModels()
				.AddViews()
				.AddConfig();
		}
	}
}
