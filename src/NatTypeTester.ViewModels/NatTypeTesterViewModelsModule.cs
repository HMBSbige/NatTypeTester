global using DynamicData;
global using DynamicData.Binding;
global using JetBrains.Annotations;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Localization;
global using NatTypeTester.Application;
global using NatTypeTester.Application.Contracts;
global using NatTypeTester.Domain.Shared.Localization;
global using ReactiveUI;
global using ReactiveUI.SourceGenerators;
global using Splat;
global using STUN;
global using STUN.Enums;
global using STUN.StunResult;
global using System.Reactive;
global using System.Reactive.Linq;
global using Volo.Abp.DependencyInjection;
global using Volo.Abp.Modularity;

namespace NatTypeTester.ViewModels;

[DependsOn(typeof(NatTypeTesterApplicationModule))]
[UsedImplicitly]
public class NatTypeTesterViewModelsModule : AbpModule;

