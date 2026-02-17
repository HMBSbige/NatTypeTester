global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Options;
global using NatTypeTester.Domain;
global using NatTypeTester.Domain.Configuration;
global using NatTypeTester.Domain.Shared.Configuration;
global using System.Text.Json;
global using System.Text.Json.Nodes;
global using System.Text.Json.Serialization;
global using Volo.Abp.Modularity;

namespace NatTypeTester.Configuration;

[DependsOn(typeof(NatTypeTesterDomainModule))]
public class NatTypeTesterConfigurationModule : AbpModule
{
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		string configPath = Path.Combine
		(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			nameof(NatTypeTester),
			ConfigurationConsts.ConfigFileName
		);

		IConfigurationRoot configuration = new ConfigurationBuilder()
			.AddJsonFile
			(source =>
				{
					source.Path = configPath;
					source.Optional = true;
					source.ReloadOnChange = false;
					source.OnLoadException = exceptionContext => exceptionContext.Ignore = true;
					source.ResolveFileProvider();
				}
			)
			.Build();

		context.Services.Configure<AppConfig>(configuration);

		context.Services.AddSingleton<IAppConfigManager>
		(sp =>
			new AppConfigManager(sp.GetRequiredService<IOptions<AppConfig>>(), configPath)
		);
	}

	public override void PostConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.PostConfigure<AppConfig>
		(config =>
			{
				if (config.StunServers is [])
				{
					config.StunServers = [.. ConfigurationConsts.DefaultStunServers];
				}
			}
		);
	}
}
