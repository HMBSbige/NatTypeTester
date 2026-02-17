namespace NatTypeTester.Domain.Configuration;

public class AppConfig
{
	public string? Language { get; set; }

	public ProxyType ProxyType { get; set; }

	public string? ProxyServer { get; set; }

	public string? ProxyUser { get; set; }

	public string? ProxyPassword { get; set; }

	public string? CurrentStunServer { get; set; }

	public List<string> StunServers { get; set; } = [];
}
