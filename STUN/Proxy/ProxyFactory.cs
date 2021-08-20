using Microsoft;
using STUN.Enums;
using System.Net;

namespace STUN.Proxy
{
	public static class ProxyFactory
	{
		public static IUdpProxy CreateProxy(
			ProxyType type, IPEndPoint? local,
			IPEndPoint? proxy = default, string? user = default, string? password = default)
		{
			switch (type)
			{
				case ProxyType.Plain:
				{
					return new NoneUdpProxy(local);
				}
				case ProxyType.Socks5:
				{
					Requires.Argument(proxy is not null, nameof(proxy), @"Proxy server is null");
					return new Socks5UdpProxy(local, proxy, user, password);
				}
				default:
				{
					throw Assumes.NotReachable();
				}
			}
		}
	}
}
