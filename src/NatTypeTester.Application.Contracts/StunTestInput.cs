namespace NatTypeTester.Application.Contracts;

public sealed record StunTestInput(
	string StunServer,
	ProxyType ProxyType,
	string ProxyServer,
	string? ProxyUser,
	string? ProxyPassword);
