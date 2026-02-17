global using JetBrains.Annotations;
global using NatTypeTester.Domain.Shared;
global using STUN.Enums;
global using Volo.Abp.Domain;
global using Volo.Abp.Modularity;

namespace NatTypeTester.Domain;

[UsedImplicitly]
[DependsOn
(
	typeof(AbpDddDomainModule),
	typeof(NatTypeTesterDomainSharedModule)
)]
public class NatTypeTesterDomainModule : AbpModule;
