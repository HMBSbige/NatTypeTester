await using ServiceProvider serviceProvider = new ServiceCollection()
	.AddNatTypeTesterApplication()
	.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

IServiceProvider sp = serviceProvider;

// Common options
Option<string> serverOption = new("--server", "-s")
{
	Required = true,
	Recursive = true,
	Description = NatTypeTesterLanguage.Current.StunServer
};
Option<string?> localOption = new("--local", "-l")
{
	Recursive = true,
	Description = NatTypeTesterLanguage.Current.LocalEnd
};
Option<string?> proxyOption = new("--proxy")
{
	Recursive = true,
	Description = NatTypeTesterLanguage.Current.SOCKS5Proxy
};
Option<string?> proxyUserOption = new("--proxy-user")
{
	Recursive = true,
	Description = NatTypeTesterLanguage.Current.ProxyUsername
};
Option<string?> proxyPasswordOption = new("--proxy-password")
{
	Recursive = true,
	Description = NatTypeTesterLanguage.Current.ProxyPassword
};

// RFC 5780 specific options
Option<bool> skipCertOption = new("--skip-cert")
{
	Description = NatTypeTesterLanguage.Current.SkipCertificateValidation
};
Option<TransportType> transportOption = new("--transport", "-t")
{
	Description = NatTypeTesterLanguage.Current.TransportProtocol,
	DefaultValueFactory = _ => TransportType.Udp
};
Option<StunTestType> testTypeOption = new("--test-type")
{
	Description = NatTypeTesterLanguage.Current.TestType,
	DefaultValueFactory = _ => StunTestType.Combining
};

// rfc3489 subcommand
Command rfc3489Command = new("rfc3489", NatTypeTesterLanguage.Current.RFC3489Description);

rfc3489Command.SetAction
(async (result, cancellationToken) =>
	{
		StunTestInput input = BuildStunTestInput(result);

		ClassicStunResult result3489 = await AnsiConsole.Status()
			.StartAsync(NatTypeTesterLanguage.Current.Testing, _ => sp.GetRequiredService<IRfc3489AppService>().TestAsync(input, cancellationToken));

		cancellationToken.ThrowIfCancellationRequested();

		ShowResultTable
		(
			[
				(NatTypeTesterLanguage.Current.NatType, result3489.NatType, "cyan"),
				(NatTypeTesterLanguage.Current.PublicEnd, result3489.PublicEndPoint, "green"),
				(NatTypeTesterLanguage.Current.LocalEnd, result3489.LocalEndPoint, "yellow")
			]
		);
	}
);

// rfc5780 subcommand
Command rfc5780Command = new("rfc5780", NatTypeTesterLanguage.Current.RFC5780Description)
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
				NatTypeTesterLanguage.Current.Testing,
				_ => testType switch
				{
					StunTestType.Binding => service.BindingTestAsync(input, transport, cancellationToken),
					StunTestType.Mapping => service.MappingBehaviorTestAsync(input, transport, cancellationToken),
					StunTestType.Filtering => service.FilteringBehaviorTestAsync(input, transport, cancellationToken),
					_ => service.TestAsync(input, transport, cancellationToken)
				}
			);

		cancellationToken.ThrowIfCancellationRequested();

		ShowResultTable(GetRows());
		return;

		IEnumerable<(string, object?, string)> GetRows()
		{
			if (testType is StunTestType.Combining or StunTestType.Binding)
			{
				yield return (NatTypeTesterLanguage.Current.BindingTest, result5780.BindingTestResult, "cyan");
			}

			if (testType is StunTestType.Combining or StunTestType.Mapping)
			{
				yield return (NatTypeTesterLanguage.Current.MappingBehavior, result5780.MappingBehavior, "magenta");
			}

			bool shouldShowFilteringBehavior = testType is StunTestType.Filtering
				|| testType is StunTestType.Combining && transport is TransportType.Udp;

			if (shouldShowFilteringBehavior)
			{
				yield return (NatTypeTesterLanguage.Current.FilteringBehavior, result5780.FilteringBehavior, "blue");
			}

			yield return (NatTypeTesterLanguage.Current.PublicEnd, result5780.PublicEndPoint, "green");
			yield return (NatTypeTesterLanguage.Current.LocalEnd, result5780.LocalEndPoint, "yellow");
		}
	}
);

RootCommand rootCommand = new(NatTypeTesterLanguage.Current.AppDescription)
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
	// Report action failures here: the default handler prints the full stack trace and says nothing on cancellation
	return await rootCommand.Parse(args).InvokeAsync(new InvocationConfiguration { EnableDefaultExceptionHandler = false });
}
catch (OperationCanceledException)
{
	AnsiConsole.MarkupLineInterpolated($"[yellow]{NatTypeTesterLanguage.Current.Cancelled}[/]");
	return 1;
}
catch (Exception ex)
{
	AnsiConsole.MarkupLineInterpolated($"[red]{NatTypeTesterLanguage.Current.Error}:[/] {ex.Message}");
	return 1;
}

void ShowResultTable(IEnumerable<(string Property, object? Value, string Color)> rows)
{
	Table table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn($"[bold]{NatTypeTesterLanguage.Current.Property.ToString().EscapeMarkup()}[/]")
		.AddColumn($"[bold]{NatTypeTesterLanguage.Current.Value.ToString().EscapeMarkup()}[/]");

	foreach ((string property, object? value, string color) in rows)
	{
		table.AddRow(property.EscapeMarkup(), $"[{color}]{value?.ToString()?.EscapeMarkup() ?? string.Empty}[/]");
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
		Proxy = new ProxyOptions
		{
			Type = proxy is not null ? ProxyType.Socks5 : ProxyType.Plain,
			Server = proxy,
			UserName = result.GetValue(proxyUserOption),
			Password = result.GetValue(proxyPasswordOption)
		},
		SkipCertificateValidation = result.GetValue(skipCertOption)
	};
}
