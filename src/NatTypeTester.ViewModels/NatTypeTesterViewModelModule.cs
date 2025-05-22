using Dns.Net.Abstractions;
using Dns.Net.Clients;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NatTypeTester.Models;
using Volo.Abp.Modularity;

namespace NatTypeTester.ViewModels;

[DependsOn(typeof(NatTypeTesterModelsModule))]
[UsedImplicitly]
public class NatTypeTesterViewModelModule : AbpModule
{
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.TryAddTransient<IDnsClient, DefaultDnsClient>();
		context.Services.TryAddTransient<DefaultAClient>();
		context.Services.TryAddTransient<DefaultAAAAClient>();
	}
}
