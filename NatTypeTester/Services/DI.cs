using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using System;

namespace NatTypeTester.Services
{
	public static class DI
	{
		public static T GetRequiredService<T>()
		{
			var service = Locator.Current.GetService<T>();

			if (service is null)
			{
				throw new InvalidOperationException($@"No service for type {typeof(T)} has been registered.");
			}

			return service;
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
