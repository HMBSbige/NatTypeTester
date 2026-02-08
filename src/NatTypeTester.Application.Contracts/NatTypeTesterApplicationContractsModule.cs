global using JetBrains.Annotations;
global using NatTypeTester.Domain.Shared;
global using Volo.Abp.Application;
global using Volo.Abp.Modularity;

namespace NatTypeTester.Application.Contracts;

[UsedImplicitly]
[DependsOn(
	typeof(AbpDddApplicationContractsModule),
	typeof(NatTypeTesterDomainSharedModule)
)]
public class NatTypeTesterApplicationContractsModule : AbpModule;
