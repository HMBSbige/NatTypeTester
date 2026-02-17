global using JetBrains.Annotations;
global using NatTypeTester.Domain.Shared.Localization;
global using System.Collections.Immutable;
global using System.Globalization;
global using Volo.Abp.Localization;
global using Volo.Abp.Modularity;
global using Volo.Abp.VirtualFileSystem;

namespace NatTypeTester.Domain.Shared;

[UsedImplicitly]
[DependsOn(typeof(AbpLocalizationModule))]
public class NatTypeTesterDomainSharedModule : AbpModule
{
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		Configure<AbpVirtualFileSystemOptions>(options => options.FileSets.AddEmbedded<NatTypeTesterDomainSharedModule>(typeof(NatTypeTesterDomainSharedModule).Namespace));

		Configure<AbpLocalizationOptions>
		(options =>
			{
				options.DefaultResourceType = typeof(NatTypeTesterResource);

				options.Resources
					.Add<NatTypeTesterResource>("en")
					.AddVirtualJson("/Localization/NatTypeTester");

				foreach (string cultureName in SupportedCultures.All)
				{
					CultureInfo cultureInfo = new(cultureName);
					options.Languages.Add(new LanguageInfo(cultureName, cultureName, cultureInfo.NativeName));
				}
			}
		);
	}
}
