using JetBrains.Annotations;
using NatTypeTester.Models;
using NatTypeTester.ViewModels;
using ReactiveUI;
using Splat;
using Splat.Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace NatTypeTester
{
	[DependsOn(
		typeof(AbpAutofacModule),
		typeof(NatTypeTesterModelsModule),
		typeof(NatTypeTesterViewModelModule)
		)]
	[UsedImplicitly]
	public class NatTypeTesterModule : AbpModule
	{
		public override void PreConfigureServices(ServiceConfigurationContext context)
		{
			context.Services.UseMicrosoftDependencyResolver();
			Locator.CurrentMutable.InitializeSplat();
			Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.Wpf);
		}
	}
}
