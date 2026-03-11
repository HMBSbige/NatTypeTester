using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NatTypeTester.Application.Contracts;
using NatTypeTester.Console;
using NatTypeTester.Domain.Shared.Localization;
using Spectre.Console;
using STUN.Enums;
using STUN.StunResult;
using System.CommandLine;
using Volo.Abp;

using IAbpApplicationWithInternalServiceProvider application = await AbpApplicationFactory.CreateAsync<NatTypeTesterConsoleModule>(options => options.UseAutofac());
await application.InitializeAsync();

IServiceProvider sp = application.ServiceProvider;
IStringLocalizer localizer = sp.GetRequiredService<IStringLocalizer<NatTypeTesterResource>>();

// Common options
Option<string> serverOption = new("--server", "-s")
{
	Required = true,
	Recursive = true,
	Description = localizer["StunServer"]
};
Option<string?> localOption = new("--local", "-l")
{
	Recursive = true,
	Description = localizer["LocalEnd"]
};
Option<string?> proxyOption = new("--proxy")
{
	Recursive = true,
	Description = localizer["SOCKS5Proxy"]
};
Option<string?> proxyUserOption = new("--proxy-user")
{
	Recursive = true,
	Description = localizer["ProxyUsername"]
};
Option<string?> proxyPasswordOption = new("--proxy-password")
{
	Recursive = true,
	Description = localizer["ProxyPassword"]
};

// RFC 5780 specific options
Option<bool> skipCertOption = new("--skip-cert")
{
	Description = localizer["SkipCertificateValidation"],
	DefaultValueFactory = _ => false
};
Option<TransportType> transportOption = new("--transport", "-t")
{
	Description = localizer["TransportProtocol"],
	DefaultValueFactory = _ => TransportType.Udp
};
Option<StunTestType> testTypeOption = new("--test-type")
{
	Description = localizer["TestType"],
	DefaultValueFactory = _ => StunTestType.Combining
};

// rfc3489 subcommand
Command rfc3489Command = new("rfc3489", localizer["RFC3489Description"]);

rfc3489Command.SetAction
(async (result, cancellationToken) =>
	{
		StunTestInput input = BuildStunTestInput(result);

		ClassicStunResult result3489 = await AnsiConsole.Status()
			.StartAsync(localizer["Testing"], _ => sp.GetRequiredService<IRfc3489AppService>().TestAsync(input, cancellationToken));

		if (cancellationToken.IsCancellationRequested)
		{
			AnsiConsole.MarkupLine($"[yellow]{localizer["Cancelled"].Value.EscapeMarkup()}[/]");
			return;
		}

		ShowResultTable
		(
			[
				(localizer["NatType"], result3489.NatType, "cyan"),
				(localizer["PublicEnd"], result3489.PublicEndPoint, "green"),
				(localizer["LocalEnd"], result3489.LocalEndPoint, "yellow")
			]
		);
	}
);

// rfc5780 subcommand
Command rfc5780Command = new("rfc5780", localizer["RFC5780Description"])
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

		IRfc5780AppService service = sp.GetRequiredService<IRfc5780AppService>();

		StunResult5389 result5780 = await AnsiConsole.Status()
			.StartAsync
			(
				localizer["Testing"],
				_ => testType switch
				{
					StunTestType.Binding => service.BindingTestAsync(input, transport, cancellationToken),
					StunTestType.Mapping => service.MappingBehaviorTestAsync(input, transport, cancellationToken),
					StunTestType.Filtering => service.FilteringBehaviorTestAsync(input, transport, cancellationToken),
					_ => service.TestAsync(input, transport, cancellationToken)
				}
			);

		if (cancellationToken.IsCancellationRequested)
		{
			AnsiConsole.MarkupLine($"[yellow]{localizer["Cancelled"].Value.EscapeMarkup()}[/]");
			return;
		}

		ShowResultTable(GetRows());
		return;

		IEnumerable<(string, object?, string)> GetRows()
		{
			if (testType is StunTestType.Combining or StunTestType.Binding)
			{
				yield return (localizer["BindingTest"], result5780.BindingTestResult, "cyan");
			}

			if (testType is StunTestType.Combining or StunTestType.Mapping)
			{
				yield return (localizer["MappingBehavior"], result5780.MappingBehavior, "magenta");
			}

			if (testType is StunTestType.Filtering
				|| testType is StunTestType.Combining && transport is TransportType.Udp
				)
			{
				yield return (localizer["FilteringBehavior"], result5780.FilteringBehavior, "blue");
			}

			yield return (localizer["PublicEnd"], result5780.PublicEndPoint, "green");
			yield return (localizer["LocalEnd"], result5780.LocalEndPoint, "yellow");
		}
	}
);

RootCommand rootCommand = new(localizer["AppDescription"])
{
	serverOption,
	localOption,
	proxyOption,
	proxyUserOption,
	proxyPasswordOption,
	rfc3489Command,
	rfc5780Command
};

try
{
	ParseResult parseResult = rootCommand.Parse(args);
	return await parseResult.InvokeAsync();
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
	AnsiConsole.MarkupLine($"[red]{localizer["Error"].Value.EscapeMarkup()}:[/] {ex.Message.EscapeMarkup()}");
	return 1;
}
finally
{
	await application.ShutdownAsync();
}

void ShowResultTable(IEnumerable<(string Property, object? Value, string Color)> rows)
{
	Table table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn($"[bold]{localizer["Property"].Value.EscapeMarkup()}[/]")
		.AddColumn($"[bold]{localizer["Value"].Value.EscapeMarkup()}[/]");

	foreach ((string property, object? value, string color) in rows)
	{
		table.AddRow(property.EscapeMarkup(), $"[{color}]{value?.ToString().EscapeMarkup()}[/]");
	}

	AnsiConsole.Write(table);
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
