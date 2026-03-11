using Socks5.Models;
using STUN.Enums;
using System.Net;

namespace STUN.Proxy;

public static class ProxyFactory
{
	public static IUdpProxy CreateProxy(TransportType transport, ProxyType type, IPEndPoint local, Socks5CreateOption? option, string targetHost, bool skipCertificateValidation = false)
	{
		return (transport, type) switch
		{
			(TransportType.Udp, ProxyType.Plain) => new NoneUdpProxy(local),
			(TransportType.Udp, ProxyType.Socks5) => new Socks5UdpProxy(local, GetSocks5Option(option)),
			(TransportType.Dtls, ProxyType.Plain) => new DtlsProxy(new NoneUdpProxy(local), targetHost, skipCertificateValidation),
			(TransportType.Dtls, ProxyType.Socks5) => new DtlsProxy(new Socks5UdpProxy(local, GetSocks5Option(option)), targetHost, skipCertificateValidation),
			_ => throw new NotSupportedException()
		};
	}

	public static ITcpProxy CreateProxy(TransportType transport, ProxyType type, Socks5CreateOption? option, string targetHost, bool skipCertificateValidation = false)
	{
		return (transport, type) switch
		{
			(TransportType.Tcp, ProxyType.Plain) => new DirectTcpProxy(),
			(TransportType.Tcp, ProxyType.Socks5) => new Socks5TcpProxy(GetSocks5Option(option)),
			(TransportType.Tls, ProxyType.Plain) => new TlsProxy(targetHost, skipCertificateValidation),
			(TransportType.Tls, ProxyType.Socks5) => new TlsOverSocks5Proxy(GetSocks5Option(option), targetHost, skipCertificateValidation),
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
