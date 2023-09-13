using JetBrains.Annotations;
using ReactiveUI;
using STUN.Enums;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.Models;

[UsedImplicitly]
public record Config : ReactiveRecord, ISingletonDependency
{
	private string _stunServer = @"stunserver.stunprotocol.org";
	public string StunServer
	{
		get => _stunServer;
		set => this.RaiseAndSetIfChanged(ref _stunServer, value);
	}

	private ProxyType _proxyType = ProxyType.Plain;
	public ProxyType ProxyType
	{
		get => _proxyType;
		set => this.RaiseAndSetIfChanged(ref _proxyType, value);
	}

	private string _proxyServer = @"127.0.0.1:1080";
	public string ProxyServer
	{
		get => _proxyServer;
		set => this.RaiseAndSetIfChanged(ref _proxyServer, value);
	}

	private string? _proxyUser;
	public string? ProxyUser
	{
		get => _proxyUser;
		set => this.RaiseAndSetIfChanged(ref _proxyUser, value);
	}

	private string? _proxyPassword;
	public string? ProxyPassword
	{
		get => _proxyPassword;
		set => this.RaiseAndSetIfChanged(ref _proxyPassword, value);
	}
}
