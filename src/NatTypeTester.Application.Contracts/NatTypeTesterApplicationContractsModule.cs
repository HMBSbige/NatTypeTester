global using JetBrains.Annotations;
global using NatTypeTester.Domain.Shared;
global using STUN;
global using STUN.Enums;
global using STUN.StunResult;
global using Volo.Abp.Application;
global using Volo.Abp.Application.Services;
global using Volo.Abp.Modularity;

namespace NatTypeTester.Application.Contracts;

[UsedImplicitly]
[DependsOn(
	typeof(AbpDddApplicationContractsModule),
	typeof(NatTypeTesterDomainSharedModule)
)]
public class NatTypeTesterApplicationContractsModule : AbpModule;
