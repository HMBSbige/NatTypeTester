global using Dns.Net.Abstractions;
global using Dns.Net.Clients;
global using JetBrains.Annotations;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using NatTypeTester.Application.Contracts;
global using NatTypeTester.Domain;
global using ReactiveUI;
global using Socks5.Models;
global using STUN;
global using STUN.Client;
global using STUN.Enums;
global using STUN.Proxy;
global using STUN.StunResult;
global using System.Net;
global using System.Net.Sockets;
global using System.Reactive.Linq;
global using Volo.Abp.Application;
global using Volo.Abp.DependencyInjection;
global using Volo.Abp.Modularity;

namespace NatTypeTester.Application;

[UsedImplicitly]
[DependsOn(
	typeof(AbpDddApplicationModule),
	typeof(NatTypeTesterApplicationContractsModule),
	typeof(NatTypeTesterDomainModule)
)]
public class NatTypeTesterApplicationModule : AbpModule
{
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.TryAddTransient<IDnsClient, DefaultDnsClient>();
		context.Services.TryAddTransient<DefaultAClient>();
		context.Services.TryAddTransient<DefaultAAAAClient>();
	}
}
