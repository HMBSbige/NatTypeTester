using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using NatTypeTester.Application.Contracts;
using NatTypeTester.Console;
using NatTypeTester.Domain.Shared;
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

string Localize(string key)
{
	return localizer[key].Value;
}

string EscapeMarkup(object? value)
{
	return value?.ToString()?.EscapeMarkup() ?? string.Empty;
}

// Common options
Option<string> serverOption = new("--server", "-s")
{
	Required = true,
	Recursive = true,
	Description = Localize("StunServer")
};
Option<string?> localOption = new("--local", "-l")
{
	Recursive = true,
	Description = Localize("LocalEnd")
};
Option<string?> proxyOption = new("--proxy")
{
	Recursive = true,
	Description = Localize("SOCKS5Proxy")
};
Option<string?> proxyUserOption = new("--proxy-user")
{
	Recursive = true,
	Description = Localize("ProxyUsername")
};
Option<string?> proxyPasswordOption = new("--proxy-password")
{
	Recursive = true,
	Description = Localize("ProxyPassword")
};

// RFC 5780 specific options
Option<bool> skipCertOption = new("--skip-cert")
{
	Description = Localize("SkipCertificateValidation"),
	DefaultValueFactory = _ => false
};
Option<TransportType> transportOption = new("--transport", "-t")
{
	Description = Localize("TransportProtocol"),
	DefaultValueFactory = _ => TransportType.Udp
};
Option<StunTestType> testTypeOption = new("--test-type")
{
	Description = Localize("TestType"),
	DefaultValueFactory = _ => StunTestType.Combining
};

// rfc3489 subcommand
Command rfc3489Command = new("rfc3489", Localize("RFC3489Description"));

rfc3489Command.SetAction
(async (result, cancellationToken) =>
	{
		StunTestInput input = BuildStunTestInput(result);

		ClassicStunResult result3489 = await AnsiConsole.Status()
			.StartAsync(Localize("Testing"), _ => sp.GetRequiredService<IRfc3489AppService>().TestAsync(input, cancellationToken));

		if (cancellationToken.IsCancellationRequested)
		{
			AnsiConsole.MarkupLineInterpolated($"[yellow]{Localize("Cancelled")}[/]");
			return;
		}

		ShowResultTable
		(
			[
				(Localize("NatType"), result3489.NatType, "cyan"),
				(Localize("PublicEnd"), result3489.PublicEndPoint, "green"),
				(Localize("LocalEnd"), result3489.LocalEndPoint, "yellow")
			]
		);
	}
);

// rfc5780 subcommand
Command rfc5780Command = new("rfc5780", Localize("RFC5780Description"))
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
				Localize("Testing"),
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
			AnsiConsole.MarkupLineInterpolated($"[yellow]{Localize("Cancelled")}[/]");
			return;
		}

		ShowResultTable(GetRows());
		return;

		IEnumerable<(string, object?, string)> GetRows()
		{
			if (testType is StunTestType.Combining or StunTestType.Binding)
			{
				yield return (Localize("BindingTest"), result5780.BindingTestResult, "cyan");
			}

			if (testType is StunTestType.Combining or StunTestType.Mapping)
			{
				yield return (Localize("MappingBehavior"), result5780.MappingBehavior, "magenta");
			}

			bool shouldShowFilteringBehavior = testType is StunTestType.Filtering
				|| testType is StunTestType.Combining && transport is TransportType.Udp;

			if (shouldShowFilteringBehavior)
			{
				yield return (Localize("FilteringBehavior"), result5780.FilteringBehavior, "blue");
			}

			yield return (Localize("PublicEnd"), result5780.PublicEndPoint, "green");
			yield return (Localize("LocalEnd"), result5780.LocalEndPoint, "yellow");
		}
	}
);

RootCommand rootCommand = new(Localize("AppDescription"))
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
	AnsiConsole.MarkupLineInterpolated($"[red]{Localize("Error")}:[/] {ex.Message}");
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
		.AddColumn($"[bold]{Localize("Property").EscapeMarkup()}[/]")
		.AddColumn($"[bold]{Localize("Value").EscapeMarkup()}[/]");

	foreach ((string property, object? value, string color) in rows)
	{
		table.AddRow(property.EscapeMarkup(), $"[{color}]{EscapeMarkup(value)}[/]");
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
