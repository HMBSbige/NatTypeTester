using ReactiveUI;
using ReactiveUI.SourceGenerators;
using STUN.Enums;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.Application.Contracts;

[UsedImplicitly]
public sealed partial class Config : ReactiveObject, ISingletonDependency
{
	public Config()
	{
		StunServer = "";
		ProxyType = ProxyType.Plain;
		ProxyServer = "127.0.0.1:1080";
		Language = string.Empty; // Follow system by default
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

	/// <summary>
	/// Language setting. Empty = follow system, "en" = English, "zh-Hans" = Simplified Chinese.
	/// </summary>
	[Reactive]
	public partial string Language { get; set; }
}
