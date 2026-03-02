namespace NatTypeTester.Domain.Configuration;

public record AppConfig
{
	public string? Language { get; set; }

	public ProxyType ProxyType { get; set; }

	public string? ProxyServer { get; set; }

	public string? ProxyUser { get; set; }

	public string? ProxyPassword { get; set; }

	public string? CurrentStunServer { get; set; }

	public List<string> StunServers { get; set; } = [];

	public string? StunServerListUri { get; set; }

	public bool AutoCheckUpdate { get; set; } = true;

	public TimeSpan CheckUpdateInterval { get; set; } = TimeSpan.FromHours(1);

	public bool IncludePreRelease { get; set; }

	public DateTimeOffset? LastUpdateCheckTime { get; set; }
}
