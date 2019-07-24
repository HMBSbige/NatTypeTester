using LumiSoft.Net.STUN.Client;
using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace NatTypeTester.Utils
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
					MessageBox.Show(@"Please specify STUN server !", @"Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return (string.Empty, DefaultLocalEnd, string.Empty);
				}

				using (var socketV4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
				{
					var ipe = ParseEndpoint(local) ?? new IPEndPoint(IPAddress.Any, 0);
					socketV4.Bind(ipe);
					var result = STUN_Client.Query(server, port, socketV4);

					return (
							result.NetType.ToString(),
							socketV4.LocalEndPoint.ToString(),
							result.NetType != STUN_NetType.UdpBlocked ? result.PublicEndPoint.ToString() : string.Empty
					);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($@"Error: {ex}", @"Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return (string.Empty, DefaultLocalEnd, string.Empty);
			}
		}
	}
}
