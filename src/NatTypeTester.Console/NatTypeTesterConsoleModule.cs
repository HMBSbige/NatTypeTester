using JetBrains.Annotations;
using NatTypeTester.Application;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace NatTypeTester.Console;

[DependsOn
(
	typeof(AbpAutofacModule),
	typeof(NatTypeTesterApplicationModule)
)]
[UsedImplicitly]
internal class NatTypeTesterConsoleModule : AbpModule;
