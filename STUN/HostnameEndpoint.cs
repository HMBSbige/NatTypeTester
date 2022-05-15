using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace STUN;

public class HostnameEndpoint
{
	public string Hostname { get; }
	public ushort Port { get; }

	private HostnameEndpoint(string host, ushort port)
	{
		Hostname = host;
		Port = port;
	}

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

	public override string ToString()
	{
		if (IPAddress.TryParse(Hostname, out IPAddress? ip) && ip.AddressFamily is AddressFamily.InterNetworkV6)
		{
			return $@"[{ip}]:{Port}";
		}

		return $@"{Hostname}:{Port}";
	}
}
