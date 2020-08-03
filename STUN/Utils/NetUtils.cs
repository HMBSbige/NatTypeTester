using STUN.Client;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace STUN.Utils
{
    public static class NetUtils
    {
        #region static method IsPrivateIP

        /// <summary>
        /// Gets if specified IP address is private LAN IP address. For example 192.168.x.x is private ip.
        /// </summary>
        /// <param name="ip">IP address to check.</param>
        /// <returns>Returns true if IP is private IP.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null reference.</exception>
        public static bool IsPrivateIP(IPAddress ip)
        {
            if (ip == null)
            {
                throw new ArgumentNullException(nameof(ip));
            }

            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                var ipBytes = ip.GetAddressBytes();

                /* Private IPs:
					First Octet = 192 AND Second Octet = 168 (Example: 192.168.X.X) 
					First Octet = 172 AND (Second Octet >= 16 AND Second Octet <= 31) (Example: 172.16.X.X - 172.31.X.X)
					First Octet = 10 (Example: 10.X.X.X)
					First Octet = 169 AND Second Octet = 254 (Example: 169.254.X.X)

				*/

                if (ipBytes[0] == 192 && ipBytes[1] == 168)
                {
                    return true;
                }
                if (ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31)
                {
                    return true;
                }
                if (ipBytes[0] == 10)
                {
                    return true;
                }
                if (ipBytes[0] == 169 && ipBytes[1] == 254)
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region static method CreateSocket

        /// <summary>
        /// Creates new socket for the specified end point.
        /// </summary>
        /// <param name="localEP">Local end point.</param>
        /// <param name="protocolType">Protocol type.</param>
        /// <returns>Return newly created socket.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>localEP</b> is null reference.</exception>
        public static Socket CreateSocket(IPEndPoint localEP, ProtocolType protocolType)
        {
            if (localEP == null)
            {
                throw new ArgumentNullException(nameof(localEP));
            }

            var socketType = SocketType.Stream;
            if (protocolType == ProtocolType.Udp)
            {
                socketType = SocketType.Dgram;
            }

            if (localEP.AddressFamily == AddressFamily.InterNetwork)
            {
                var socket = new Socket(AddressFamily.InterNetwork, socketType, protocolType);
                socket.Bind(localEP);

                return socket;
            }

            if (localEP.AddressFamily == AddressFamily.InterNetworkV6)
            {
                var socket = new Socket(AddressFamily.InterNetworkV6, socketType, protocolType);
                socket.Bind(localEP);

                return socket;
            }

            throw new ArgumentException(@"Invalid IPEndPoint address family.");
        }

        #endregion

        public const string DefaultLocalEnd = @"0.0.0.0:0";

        public static IPEndPoint ParseEndpoint(string str)
        {
            //TODO:IPv6
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

                using var socketV4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                var ipe = ParseEndpoint(local) ?? new IPEndPoint(IPAddress.Any, 0);
                socketV4.Bind(ipe);
                var result = StunClient.Query(server, port, socketV4);

                return (
                        result.NatType.ToString(),
                        socketV4.LocalEndPoint.ToString(),
                        result.NatType != NatType.UdpBlocked ? result.PublicEndPoint.ToString() : string.Empty
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($@"[ERROR]: {ex}");
                return (string.Empty, DefaultLocalEnd, string.Empty);
            }
        }
    }
}
