using Microsoft;
using Socks5.Models;
using STUN.Enums;
using System.Net;

namespace STUN.Proxy;

public static class ProxyFactory
{
	public static IUdpProxy CreateProxy(ProxyType type, IPEndPoint local, Socks5CreateOption option)
	{
		switch (type)
		{
			case ProxyType.Plain:
			{
				return new NoneUdpProxy(local);
			}
			case ProxyType.Socks5:
			{
				Requires.NotNull(option, nameof(option));
				Requires.Argument(option.Address is not null, nameof(option), @"Proxy server is null");
				return new Socks5UdpProxy(local, option);
			}
			default:
			{
				throw Assumes.NotReachable();
			}
		}
	}

	public static ITcpProxy CreateProxy(TransportType transport, ProxyType type, Socks5CreateOption option, string targetHost)
	{
		switch (transport, type)
		{
			case (TransportType.Tcp, ProxyType.Plain):
			{
				return new DirectTcpProxy();
			}
			case (TransportType.Tcp, ProxyType.Socks5):
			{
				Requires.NotNull(option, nameof(option));
				Requires.Argument(option.Address is not null, nameof(option), @"Proxy server is null");
				return new Socks5TcpProxy(option);
			}
			case (TransportType.Tls, ProxyType.Plain):
			{
				return new TlsProxy(targetHost);
			}
			case (TransportType.Tls, ProxyType.Socks5):
			{
				Requires.NotNull(option, nameof(option));
				Requires.Argument(option.Address is not null, nameof(option), @"Proxy server is null");
				return new TlsOverSocks5Proxy(option, targetHost);
			}
			default:
			{
				throw new NotSupportedException();
			}
		}
	}
}
