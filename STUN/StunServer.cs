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

		private StunServer(string hostname, ushort port)
		{
			Hostname = hostname;
			Port = port;
		}

		public static bool TryParse(string s, [NotNullWhen(true)] out StunServer? result)
		{
			if (!HostnameEndpoint.TryParse(s, out var host, DefaultPort))
			{
				result = null;
				return false;
			}

			result = new StunServer(host.Hostname, host.Port);
			return true;
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
