global using JetBrains.Annotations;
global using Microsoft;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.DependencyInjection.Extensions;
global using Microsoft.UI.Windowing;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
global using Microsoft.VisualStudio.Threading;
global using NatTypeTester.Extensions;
global using NatTypeTester.ViewModels;
global using NatTypeTester.Views;
global using ReactiveMarbles.ObservableEvents;
global using ReactiveUI;
global using Splat;
global using Splat.Microsoft.Extensions.DependencyInjection;
global using STUN.Enums;
global using System.Reactive.Disposables;
global using System.Reactive.Linq;
global using Volo.Abp;
global using Volo.Abp.Autofac;
global using Volo.Abp.DependencyInjection;
global using Volo.Abp.Modularity;
global using Windows.ApplicationModel.Resources;
global using Windows.Graphics;
global using Windows.System;

namespace NatTypeTester;

[DependsOn(
	typeof(AbpAutofacModule),
	typeof(NatTypeTesterViewModelModule)
)]
[UsedImplicitly]
internal class NatTypeTesterModule : AbpModule
{
	public override void PreConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.UseMicrosoftDependencyResolver();
		Locator.CurrentMutable.InitializeSplat();
		Locator.CurrentMutable.InitializeReactiveUI(RegistrationNamespace.WinUI);
	}

	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.TryAddTransient<RoutingState>();
	}
}
