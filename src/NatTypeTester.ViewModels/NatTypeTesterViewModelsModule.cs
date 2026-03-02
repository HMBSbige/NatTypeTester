global using DynamicData;
global using JetBrains.Annotations;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Localization;
global using NatTypeTester.Application;
global using NatTypeTester.Application.Contracts;
global using NatTypeTester.Domain.Configuration;
global using NatTypeTester.Domain.Shared;
global using NatTypeTester.Domain.Shared.Localization;
global using ReactiveUI;
global using ReactiveUI.SourceGenerators;
global using STUN;
global using STUN.Enums;
global using STUN.StunResult;
global using System.Collections.ObjectModel;
global using System.Diagnostics;
global using System.Globalization;
global using System.Net;
global using System.Reactive;
global using System.Reactive.Disposables;
global using System.Reactive.Disposables.Fluent;
global using System.Reactive.Linq;
global using System.Reactive.Subjects;
global using Volo.Abp.DependencyInjection;
global using Volo.Abp.Localization;
global using Volo.Abp.Modularity;

namespace NatTypeTester.ViewModels;

[DependsOn(typeof(NatTypeTesterApplicationModule))]
[UsedImplicitly]
public class NatTypeTesterViewModelsModule : AbpModule;
