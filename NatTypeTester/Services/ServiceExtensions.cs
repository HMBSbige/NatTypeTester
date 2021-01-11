using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NatTypeTester.Models;
using NatTypeTester.ViewModels;
using NatTypeTester.Views;
using ReactiveUI;

namespace NatTypeTester.Services
{
	public static class ServiceExtensions
	{
		public static IServiceCollection AddViewModels(this IServiceCollection services)
		{
			services.TryAddSingleton<MainWindowViewModel>();
			services.TryAddSingleton<RFC5780ViewModel>();
			services.TryAddSingleton<RFC3489ViewModel>();
			services.TryAddSingleton<SettingViewModel>();

			services.TryAddSingleton<IScreen>(provider => provider.GetRequiredService<MainWindowViewModel>());

			return services;
		}

		public static IServiceCollection AddViews(this IServiceCollection services)
		{
			services.TryAddSingleton<MainWindow>();
			services.TryAddTransient<IViewFor<RFC5780ViewModel>, RFC5780View>();
			services.TryAddTransient<IViewFor<RFC3489ViewModel>, RFC3489View>();
			services.TryAddTransient<IViewFor<SettingViewModel>, SettingView>();

			return services;
		}

		public static IServiceCollection AddConfig(this IServiceCollection services)
		{
			services.TryAddSingleton<Config>();

			return services;
		}
	}
}
