using NatTypeTester_Console.Net.STUN.Client;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace NatTypeTester_Console
{
	public static class Utils
	{
		public const string DefaultLocalEnd = @"0.0.0.0:0";

		public static IPEndPoint ParseEndpoint(string str)
		{
			var ipPort = str.Trim().Split(':');
			if (ipPort.Length == 2)
			{
				if (IPAddress.TryParse(ipPort[0], out var ip))
				{
					if (ushort.TryParse(ipPort[1], out var port))
					{
						return new IPEndPoint(ip, port);
					}
				}
			}

			return null;
		}

		public static (string, string, string) NatTypeTestCore(string local, string server, int port)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(server))
				{
					Debug.WriteLine(@"[ERROR]: Please specify STUN server !");
					return (string.Empty, DefaultLocalEnd, string.Empty);
				}

				using (var socketV4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					var ipe = ParseEndpoint(local) ?? new IPEndPoint(IPAddress.Any, 0);
					socketV4.Bind(ipe);
					var result = StunClient.Query(server, port, socketV4);

					return (
							result.NatType.ToString(),
							socketV4.LocalEndPoint.ToString(),
							result.NatType != NatType.UdpBlocked ? result.PublicEndPoint.ToString() : string.Empty
					);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine($@"[ERROR]: {ex}");
				return (string.Empty, DefaultLocalEnd, string.Empty);
			}
		}
	}
}
