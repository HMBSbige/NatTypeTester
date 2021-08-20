using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace STUN
{
	public class StunServer
	{
		public string Hostname { get; }
		public ushort Port { get; }

		private const ushort DefaultPort = 3478;

		public StunServer()
		{
			Hostname = @"stun.syncthing.net";
			Port = DefaultPort;
		}

		private StunServer(string host, ushort port)
		{
			Hostname = host;
			Port = port;
		}

		public static bool TryParse(string str, [NotNullWhen(true)] out StunServer? server)
		{
			server = null;
			if (string.IsNullOrEmpty(str))
			{
				return false;
			}

			var hostLength = str.Length;
			var pos = str.LastIndexOf(':');

			if (pos > 0)
			{
				if (str[pos - 1] is ']')
				{
					hostLength = pos;
				}
				else if (str.AsSpan(0, pos).LastIndexOf(':') is -1)
				{
					hostLength = pos;
				}
			}

			var host = str[..hostLength];
			var type = Uri.CheckHostName(host);
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

			if (hostLength == str.Length)
			{
				server = new StunServer(host, DefaultPort);
				return true;
			}

			if (ushort.TryParse(str.AsSpan(hostLength + 1), out var port))
			{
				server = new StunServer(host, port);
				return true;
			}

			return false;
		}

		public override string ToString()
		{
			if (Port is DefaultPort)
			{
				return Hostname;
			}
			if (IPAddress.TryParse(Hostname, out var ip) && ip.AddressFamily is AddressFamily.InterNetworkV6)
			{
				return $@"[{ip}]:{Port}";
			}
			return $@"{Hostname}:{Port}";
		}
	}
}
