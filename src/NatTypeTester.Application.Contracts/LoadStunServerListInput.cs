namespace NatTypeTester.Application.Contracts;

public sealed class LoadStunServerListInput
{
	public required string Uri { get; init; }

	public ProxyType ProxyType { get; init; }

	public string? ProxyServer { get; init; }

	public string? ProxyUser { get; init; }

	public string? ProxyPassword { get; init; }
}
