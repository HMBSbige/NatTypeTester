using Microsoft;
using Socks5.Models;
using STUN.Enums;
using System.Net;

namespace STUN.Proxy;

public static class ProxyFactory
{
	public static IUdpProxy CreateProxy(ProxyType type, IPEndPoint local, Socks5CreateOption option)
	{
		return type switch
		{
			ProxyType.Plain => new NoneUdpProxy(local),
			ProxyType.Socks5 => CreateSocks5UdpProxy(local, option),
			_ => throw Assumes.NotReachable()
		};

		static Socks5UdpProxy CreateSocks5UdpProxy(IPEndPoint local, Socks5CreateOption option)
		{
			Requires.NotNull(option);
			Requires.Argument(option.Address is not null, nameof(option), @"Proxy server is null");
			return new Socks5UdpProxy(local, option);
		}
	}

	public static ITcpProxy CreateProxy(TransportType transport, ProxyType type, Socks5CreateOption option, string targetHost)
	{
		return (transport, type) switch
		{
			(TransportType.Tcp, ProxyType.Plain) => new DirectTcpProxy(),
			(TransportType.Tcp, ProxyType.Socks5) => CreateSocks5TcpProxy(option),
			(TransportType.Tls, ProxyType.Plain) => new TlsProxy(targetHost),
			(TransportType.Tls, ProxyType.Socks5) => CreateTlsOverSocks5Proxy(option, targetHost),
			_ => throw new NotSupportedException()
		};

		static Socks5TcpProxy CreateSocks5TcpProxy(Socks5CreateOption option)
		{
			Requires.NotNull(option);
			Requires.Argument(option.Address is not null, nameof(option), @"Proxy server is null");
			return new Socks5TcpProxy(option);
		}

		static TlsOverSocks5Proxy CreateTlsOverSocks5Proxy(Socks5CreateOption option, string targetHost)
		{
			Requires.NotNull(option);
			Requires.Argument(option.Address is not null, nameof(option), @"Proxy server is null");
			return new TlsOverSocks5Proxy(option, targetHost);
		}
	}
}
