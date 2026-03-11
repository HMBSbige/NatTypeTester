using Microsoft.Extensions.DependencyInjection;
using NatTypeTester.Application.Contracts;
using NatTypeTester.Console;
using STUN.Enums;
using STUN.StunResult;
using System.CommandLine;
using Volo.Abp;

// Common options
Option<string> serverOption = new("--server", "-s")
{
	Required = true,
	Recursive = true,
	Description = "STUN server address"
};
Option<string?> localOption = new("--local", "-l")
{
	Recursive = true,
	Description = "Local bind address"
};
Option<string?> proxyOption = new("--proxy")
{
	Recursive = true,
	Description = "SOCKS5 proxy address"
};
Option<string?> proxyUserOption = new("--proxy-user")
{
	Recursive = true,
	Description = "SOCKS5 proxy username"
};
Option<string?> proxyPasswordOption = new("--proxy-password")
{
	Recursive = true,
	Description = "SOCKS5 proxy password"
};

// RFC 5780 specific options
Option<bool> skipCertOption = new("--skip-cert")
{
	Description = "Disable TLS/DTLS certificate validation",
	DefaultValueFactory = _ => false
};
Option<TransportType> transportOption = new("--transport", "-t")
{
	Description = "Transport protocol: Udp, Tcp, Tls, Dtls",
	DefaultValueFactory = _ => TransportType.Udp
};
Option<StunTestType> testTypeOption = new("--test-type")
{
	Description = "Test type: Combining, Binding, Filtering, Mapping",
	DefaultValueFactory = _ => StunTestType.Combining
};

// rfc3489 subcommand
Command rfc3489Command = new("rfc3489", "Test NAT type using RFC 3489 (Classic STUN)");

rfc3489Command.SetAction
(async (result, cancellationToken) =>
	{
		StunTestInput input = BuildStunTestInput(result);

		await RunWithAbpAsync(result, async (sp, output) =>
		{
			ClassicStunResult result3489 = await sp.GetRequiredService<IRfc3489AppService>().TestAsync(input, cancellationToken);

			await output.WriteLineAsync($"NAT Type: {result3489.NatType}");
			await output.WriteLineAsync($"Public EndPoint: {result3489.PublicEndPoint}");
			await output.WriteLineAsync($"Local EndPoint: {result3489.LocalEndPoint}");
		});
	}
);

// rfc5780 subcommand
Command rfc5780Command = new("rfc5780", "Test NAT type using RFC 5780")
{
	skipCertOption,
	transportOption,
	testTypeOption
};

rfc5780Command.SetAction
(async (result, cancellationToken) =>
	{
		StunTestInput input = BuildStunTestInput(result);
		TransportType transport = result.GetValue(transportOption);
		StunTestType testType = result.GetValue(testTypeOption);

		await RunWithAbpAsync(result, async (sp, output) =>
		{
			IRfc5780AppService service = sp.GetRequiredService<IRfc5780AppService>();

			StunResult5389 result5780 = testType switch
			{
				StunTestType.Binding => await service.BindingTestAsync(input, transport, cancellationToken),
				StunTestType.Mapping => await service.MappingBehaviorTestAsync(input, transport, cancellationToken),
				StunTestType.Filtering => await service.FilteringBehaviorTestAsync(input, transport, cancellationToken),
				_ => await service.TestAsync(input, transport, cancellationToken)
			};

			if (testType is StunTestType.Combining or StunTestType.Binding)
			{
				await output.WriteLineAsync($"Binding Test: {result5780.BindingTestResult}");
			}
			if (testType is StunTestType.Combining or StunTestType.Mapping)
			{
				await output.WriteLineAsync($"Mapping Behavior: {result5780.MappingBehavior}");
			}
			if (testType is StunTestType.Combining or StunTestType.Filtering)
			{
				await output.WriteLineAsync($"Filtering Behavior: {result5780.FilteringBehavior}");
			}
			await output.WriteLineAsync($"Public EndPoint: {result5780.PublicEndPoint}");
			await output.WriteLineAsync($"Local EndPoint: {result5780.LocalEndPoint}");
		});
	}
);

RootCommand rootCommand = new("NatTypeTester - NAT type testing tool")
{
	serverOption,
	localOption,
	proxyOption,
	proxyUserOption,
	proxyPasswordOption,
	rfc3489Command,
	rfc5780Command
};

ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync(cancellationToken: CancellationToken.None);

async Task RunWithAbpAsync(ParseResult result, Func<IServiceProvider, TextWriter, Task> action)
{
	using IAbpApplicationWithInternalServiceProvider application = await AbpApplicationFactory.CreateAsync<NatTypeTesterConsoleModule>(options => options.UseAutofac());
	await application.InitializeAsync();

	try
	{
		await action(application.ServiceProvider, result.InvocationConfiguration.Output);
	}
	catch (Exception ex) when (ex is not OperationCanceledException)
	{
		await result.InvocationConfiguration.Error.WriteLineAsync($"Error: {ex.Message}");
	}
	finally
	{
		await application.ShutdownAsync();
	}
}

StunTestInput BuildStunTestInput(ParseResult result)
{
	string server = result.GetRequiredValue(serverOption);
	string? proxy = result.GetValue(proxyOption);

	return new StunTestInput
	{
		StunServer = server,
		LocalEndPoint = result.GetValue(localOption),
		ProxyType = proxy is not null ? ProxyType.Socks5 : ProxyType.Plain,
		ProxyServer = proxy,
		ProxyUser = result.GetValue(proxyUserOption),
		ProxyPassword = result.GetValue(proxyPasswordOption),
		SkipCertificateValidation = result.GetValue(skipCertOption)
	};
}
