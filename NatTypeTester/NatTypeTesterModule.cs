using Dns.Net.Abstractions;
using Dns.Net.Clients;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

		public override void ConfigureServices(ServiceConfigurationContext context)
		{
			context.Services.TryAddTransient<RoutingState>();
			context.Services.TryAddTransient<IDnsClient, DefaultDnsClient>();
		}
	}
}
