namespace NatTypeTester.Application.Contracts;

public sealed class ProxyOptions
{
	public ProxyType Type { get; init; } = ProxyType.Plain;

	public string? Server { get; init; }

	public string? UserName { get; init; }

	public string? Password { get; init; }
}
