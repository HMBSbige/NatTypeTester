namespace NatTypeTester.Application.Contracts;

public sealed class LoadStunServerListInput
{
	public required string Uri { get; init; }

	public ProxyOptions Proxy { get; init; } = new();
}
