using Socks5.Models;
using STUN.Enums;
using System.Net;

namespace STUN.Proxy;

/// <summary>
/// Factory for creating TCP and UDP proxy instances based on transport and proxy type configuration.
/// </summary>
public static class ProxyFactory
{
	/// <summary>
	/// Creates a UDP proxy instance for the specified transport and proxy type.
	/// </summary>
	/// <param name="transport">The transport type (UDP or DTLS).</param>
	/// <param name="type">The proxy type (Plain or SOCKS5).</param>
	/// <param name="local">The local endpoint to bind to.</param>
	/// <param name="option">The SOCKS5 connection options, required when proxy type is SOCKS5.</param>
	/// <param name="targetHost">The target host name, used for DTLS server name indication.</param>
	/// <param name="skipCertificateValidation">Whether to skip server certificate validation for DTLS.</param>
	/// <returns>A configured <see cref="IUdpProxy"/> instance.</returns>
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

	/// <summary>
	/// Creates a TCP proxy instance for the specified transport and proxy type.
	/// </summary>
	/// <param name="transport">The transport type (TCP or TLS).</param>
	/// <param name="type">The proxy type (Plain or SOCKS5).</param>
	/// <param name="option">The SOCKS5 connection options, required when proxy type is SOCKS5.</param>
	/// <param name="targetHost">The target host name, used for TLS server name indication.</param>
	/// <param name="skipCertificateValidation">Whether to skip server certificate validation for TLS.</param>
	/// <returns>A configured <see cref="ITcpProxy"/> instance.</returns>
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
