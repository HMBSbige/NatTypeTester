using ReactiveUI.Fody.Helpers;
using STUN.Enums;

namespace NatTypeTester.Models
{
	public class Config
	{
		[Reactive]
		public string StunServer { get; set; } = @"stun.syncthing.net";

		[Reactive]
		public ProxyType ProxyType { get; set; } = ProxyType.Plain;

		[Reactive]
		public string ProxyServer { get; set; } = @"127.0.0.1:1080";

		[Reactive]
		public string? ProxyUser { get; set; }

		[Reactive]
		public string? ProxyPassword { get; set; }
	}
}
