global using JetBrains.Annotations;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Localization;
global using NatTypeTester.Domain.Shared;
global using NatTypeTester.Domain.Shared.Localization;
global using STUN;
global using STUN.Enums;
global using STUN.StunResult;
global using System.ComponentModel.DataAnnotations;
global using System.Net;
global using Volo.Abp.Application;
global using Volo.Abp.Application.Dtos;
global using Volo.Abp.Application.Services;
global using Volo.Abp.Modularity;

namespace NatTypeTester.Application.Contracts;

[UsedImplicitly]
[DependsOn
(
	typeof(AbpDddApplicationContractsModule),
	typeof(NatTypeTesterDomainSharedModule)
)]
public class NatTypeTesterApplicationContractsModule : AbpModule;
