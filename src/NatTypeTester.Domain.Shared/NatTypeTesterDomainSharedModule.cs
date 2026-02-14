global using JetBrains.Annotations;
global using NatTypeTester.Domain.Shared.Localization;
global using STUN;
global using STUN.Enums;
global using STUN.StunResult;
global using System.Globalization;
global using Volo.Abp.Localization;
global using Volo.Abp.Modularity;
global using Volo.Abp.VirtualFileSystem;
using System.Reflection;

namespace NatTypeTester.Domain.Shared;

[UsedImplicitly]
[DependsOn(typeof(AbpLocalizationModule))]
public class NatTypeTesterDomainSharedModule : AbpModule
{
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		Configure<AbpVirtualFileSystemOptions>(options => options.FileSets.AddEmbedded<NatTypeTesterDomainSharedModule>(typeof(NatTypeTesterDomainSharedModule).Namespace));

		Configure<AbpLocalizationOptions>(options =>
		{
			options.DefaultResourceType = typeof(NatTypeTesterResource);

			options.Resources
				.Add<NatTypeTesterResource>("en")
				.AddVirtualJson("/Localization/NatTypeTester");

			Assembly assembly = typeof(NatTypeTesterDomainSharedModule).Assembly;
			string prefix = $"{typeof(NatTypeTesterDomainSharedModule).Namespace}.Localization.NatTypeTester.";
			const string suffix = ".json";

			foreach (string resourceName in assembly.GetManifestResourceNames())
			{
				if (!resourceName.StartsWith(prefix) || !resourceName.EndsWith(suffix))
				{
					continue;
				}

				string cultureName = resourceName[prefix.Length..^suffix.Length];
				CultureInfo cultureInfo = new(cultureName);
				options.Languages.Add(new LanguageInfo(cultureName, cultureName, cultureInfo.NativeName));
			}
		});
	}
}
