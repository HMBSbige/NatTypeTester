namespace NatTypeTester.Models;

[UsedImplicitly]
public sealed partial class Config : ReactiveObject, ISingletonDependency
{
	public Config()
	{
		StunServer = @"";
		ProxyType = ProxyType.Plain;
		ProxyServer = @"127.0.0.1:1080";
	}

	[Reactive]
	public partial string StunServer { get; set; }

	[Reactive]
	public partial ProxyType ProxyType { get; set; }

	[Reactive]
	public partial string ProxyServer { get; set; }

	[Reactive]
	public partial string? ProxyUser { get; set; }

	[Reactive]
	public partial string? ProxyPassword { get; set; }
}
