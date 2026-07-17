namespace NatTypeTester.Application.Contracts;

public sealed class StunTestInput
{
	public required string StunServer { get; init; }

	public ProxyOptions Proxy { get; init; } = new();

	public string? LocalEndPoint { get; init; }

	public bool SkipCertificateValidation { get; init; }
}
