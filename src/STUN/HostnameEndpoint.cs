using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace STUN;

/// <summary>
/// Represents a network endpoint identified by a hostname (or IP address) and a port number.
/// Supports parsing from string representations including IPv4, IPv6, and DNS hostnames.
/// </summary>
public class HostnameEndpoint
{
	/// <summary>
	/// Gets the hostname or IP address.
	/// </summary>
	public string Hostname { get; }

	/// <summary>
	/// Gets the port number.
	/// </summary>
	public ushort Port { get; }

	private HostnameEndpoint(string host, ushort port)
	{
		Hostname = host;
		Port = port;
	}

	/// <summary>
	/// Attempts to parse a string into a <see cref="HostnameEndpoint"/> instance.
	/// Accepts formats such as "hostname", "hostname:port", "ip:port", and "[ipv6]:port".
	/// </summary>
	/// <param name="s">The string to parse.</param>
	/// <param name="result">When this method returns, contains the parsed <see cref="HostnameEndpoint"/> if successful; otherwise, <see langword="null"/>.</param>
	/// <param name="defaultPort">The default port to use if none is specified in the string.</param>
	/// <returns><see langword="true"/> if the string was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public static bool TryParse(string s, [NotNullWhen(true)] out HostnameEndpoint? result, ushort defaultPort = 0)
	{
		result = null;

		if (string.IsNullOrEmpty(s))
		{
			return false;
		}

		int hostLength = s.Length;
		int pos = s.LastIndexOf(':');

		if (pos > 0)
		{
			if (s[pos - 1] is ']')
			{
				hostLength = pos;
			}
			else if (s.AsSpan(0, pos).LastIndexOf(':') is -1)
			{
				hostLength = pos;
			}
		}

		string host = s[..hostLength];
		UriHostNameType type = Uri.CheckHostName(host);

		switch (type)
		{
			case UriHostNameType.Dns:
			case UriHostNameType.IPv4:
			case UriHostNameType.IPv6:
			{
				break;
			}
			default:
			{
				return false;
			}
		}

		if (hostLength == s.Length || ushort.TryParse(s.AsSpan(hostLength + 1), out defaultPort))
		{
			result = new HostnameEndpoint(host, defaultPort);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Returns the string representation of the endpoint in "hostname:port" format.
	/// IPv6 addresses are enclosed in brackets.
	/// </summary>
	/// <returns>A string representation of the endpoint.</returns>
	public override string ToString()
	{
		if (IPAddress.TryParse(Hostname, out IPAddress? ip) && ip.AddressFamily is AddressFamily.InterNetworkV6)
		{
			return $@"[{ip}]:{Port}";
		}

		return $@"{Hostname}:{Port}";
	}
}
