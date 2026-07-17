await using ServiceProvider serviceProvider = new ServiceCollection()
	.AddNatTypeTesterApplication()
	.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

IServiceProvider sp = serviceProvider;
ILocalizedValues language = NatTypeTesterLanguage.Current;

// Common options
Option<string> serverOption = new("--server", "-s")
{
	Required = true,
	Recursive = true,
	Description = language.StunServer.ToString()
};
Option<string?> localOption = new("--local", "-l")
{
	Recursive = true,
	Description = language.LocalEnd.ToString()
};
Option<string?> proxyOption = new("--proxy")
{
	Recursive = true,
	Description = language.SOCKS5Proxy.ToString()
};
Option<string?> proxyUserOption = new("--proxy-user")
{
	Recursive = true,
	Description = language.ProxyUsername.ToString()
};
Option<string?> proxyPasswordOption = new("--proxy-password")
{
	Recursive = true,
	Description = language.ProxyPassword.ToString()
};

// RFC 5780 specific options
Option<bool> skipCertOption = new("--skip-cert")
{
	Description = language.SkipCertificateValidation.ToString(),
	DefaultValueFactory = _ => false
};
Option<TransportType> transportOption = new("--transport", "-t")
{
	Description = language.TransportProtocol.ToString(),
	DefaultValueFactory = _ => TransportType.Udp
};
Option<StunTestType> testTypeOption = new("--test-type")
{
	Description = language.TestType.ToString(),
	DefaultValueFactory = _ => StunTestType.Combining
};

// rfc3489 subcommand
Command rfc3489Command = new("rfc3489", language.RFC3489Description.ToString());

rfc3489Command.SetAction
(async (result, cancellationToken) =>
	{
		return await ExecuteCommandAsync(async () =>
		{
			StunTestInput input = BuildStunTestInput(result);

			ClassicStunResult result3489 = await AnsiConsole.Status()
				.StartAsync(language.Testing.ToString(), _ => sp.GetRequiredService<IRfc3489AppService>().TestAsync(input, cancellationToken));

			cancellationToken.ThrowIfCancellationRequested();

			ShowResultTable
			(
				[
					(language.NatType.ToString(), result3489.NatType, "cyan"),
					(language.PublicEnd.ToString(), result3489.PublicEndPoint, "green"),
					(language.LocalEnd.ToString(), result3489.LocalEndPoint, "yellow")
				]
			);
		});
	}
);

// rfc5780 subcommand
Command rfc5780Command = new("rfc5780", language.RFC5780Description.ToString())
{
	skipCertOption,
	transportOption,
	testTypeOption
};

rfc5780Command.SetAction
(async (result, cancellationToken) =>
	{
		return await ExecuteCommandAsync(async () =>
		{
			StunTestInput input = BuildStunTestInput(result);
			TransportType transport = result.GetValue(transportOption);
			StunTestType testType = result.GetValue(testTypeOption);

			IRfc5780AppService service = sp.GetRequiredService<IRfc5780AppService>();

			StunResult5389 result5780 = await AnsiConsole.Status()
				.StartAsync
				(
					language.Testing.ToString(),
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
					yield return (language.BindingTest.ToString(), result5780.BindingTestResult, "cyan");
				}

				if (testType is StunTestType.Combining or StunTestType.Mapping)
				{
					yield return (language.MappingBehavior.ToString(), result5780.MappingBehavior, "magenta");
				}

				bool shouldShowFilteringBehavior = testType is StunTestType.Filtering
					|| testType is StunTestType.Combining && transport is TransportType.Udp;

				if (shouldShowFilteringBehavior)
				{
					yield return (language.FilteringBehavior.ToString(), result5780.FilteringBehavior, "blue");
				}

				yield return (language.PublicEnd.ToString(), result5780.PublicEndPoint, "green");
				yield return (language.LocalEnd.ToString(), result5780.LocalEndPoint, "yellow");
			}
		});
	}
);

RootCommand rootCommand = new(language.AppDescription.ToString())
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
	return ShowError(ex);
}

int ShowError(Exception ex)
{
	AnsiConsole.MarkupLineInterpolated($"[red]{language.Error}:[/] {ex.Message}");
	return 1;
}

async Task<int> ExecuteCommandAsync(Func<Task> action)
{
	try
	{
		await action();
		return 0;
	}
	catch (OperationCanceledException)
	{
		AnsiConsole.MarkupLineInterpolated($"[yellow]{language.Cancelled}[/]");
		return 1;
	}
	catch (Exception ex)
	{
		return ShowError(ex);
	}
}

void ShowResultTable(IEnumerable<(string Property, object? Value, string Color)> rows)
{
	Table table = new Table()
		.Border(TableBorder.Rounded)
		.AddColumn($"[bold]{language.Property.ToString().EscapeMarkup()}[/]")
		.AddColumn($"[bold]{language.Value.ToString().EscapeMarkup()}[/]");

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
