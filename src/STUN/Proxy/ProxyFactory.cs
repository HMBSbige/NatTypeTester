using Socks5.Models;
using STUN.Enums;
using System.Diagnostics;
using System.Net;

namespace STUN.Proxy;

public static class ProxyFactory
{
	public static IUdpProxy CreateProxy(ProxyType type, IPEndPoint local, Socks5CreateOption? option)
	{
		return type switch
		{
			ProxyType.Plain => new NoneUdpProxy(local),
			ProxyType.Socks5 => new Socks5UdpProxy(local, GetSocks5Option(option)),
			_ => throw new UnreachableException()
		};
	}

	public static ITcpProxy CreateProxy(TransportType transport, ProxyType type, Socks5CreateOption? option, string targetHost)
	{
		return (transport, type) switch
		{
			(TransportType.Tcp, ProxyType.Plain) => new DirectTcpProxy(),
			(TransportType.Tcp, ProxyType.Socks5) => new Socks5TcpProxy(GetSocks5Option(option)),
			(TransportType.Tls, ProxyType.Plain) => new TlsProxy(targetHost),
			(TransportType.Tls, ProxyType.Socks5) => new TlsOverSocks5Proxy(GetSocks5Option(option), targetHost),
			_ => throw new NotSupportedException()
		};
	}

	private static Socks5CreateOption GetSocks5Option(Socks5CreateOption? option)
	{
		ArgumentNullException.ThrowIfNull(option);
		ArgumentNullException.ThrowIfNull(option.Address, nameof(option.Address));
		return option;
	}
}
