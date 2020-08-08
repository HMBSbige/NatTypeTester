using STUN.Client;
using STUN.StunResult;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace STUN.Utils
{
    public static class NetUtils
    {
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

        public static (string, string, string) NatTypeTestCore(string local, string server, ushort port)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(server))
                {
                    Debug.WriteLine(@"[ERROR]: Please specify STUN server !");
                    return (string.Empty, DefaultLocalEnd, string.Empty);
                }

                using var client = new StunClient3489(server, port, ParseEndpoint(local));

                var result = (ClassicStunResult)client.Query();

                return (
                        result.NatType.ToString(),
                        $@"{client.LocalEndPoint}",
                        $@"{result.PublicEndPoint}"
                );
            }
            catch (Exception ex)
            {
                Debug.WriteLine($@"[ERROR]: {ex}");
                return (string.Empty, DefaultLocalEnd, string.Empty);
            }
        }

        public static (byte[], IPEndPoint, IPAddress) UdpReceive(this UdpClient client, byte[] bytes, IPEndPoint remote, EndPoint receive)
        {
            var localEndPoint = (IPEndPoint)client.Client.LocalEndPoint;

            Debug.WriteLine($@"{localEndPoint} => {remote} {bytes.Length} 字节");

            client.Send(bytes, bytes.Length, remote);

            var res = new byte[ushort.MaxValue];
            var flag = SocketFlags.None;

            var length = client.Client.ReceiveMessageFrom(res, 0, res.Length, ref flag, ref receive, out var ipPacketInformation);

            var local = ipPacketInformation.Address;

            Debug.WriteLine($@"{(IPEndPoint)receive} => {local} {length} 字节");

            return (res.Take(length).ToArray(),
                    (IPEndPoint)receive
                    , local);
        }

        public static async Task<(byte[], IPEndPoint, IPAddress)> UdpReceiveAsync(this UdpClient client, byte[] bytes, IPEndPoint remote, EndPoint receive)
        {
            var localEndPoint = (IPEndPoint)client.Client.LocalEndPoint;

            Debug.WriteLine($@"{localEndPoint} => {remote} {bytes.Length} 字节");

            await client.SendAsync(bytes, bytes.Length, remote);
            var res = new ArraySegment<byte>(new byte[ushort.MaxValue]);

            var result = await client.Client.ReceiveMessageFromAsync(res, SocketFlags.None, receive);

            var local = result.PacketInformation.Address;

            Debug.WriteLine($@"{(IPEndPoint)result.RemoteEndPoint} => {local} {result.ReceivedBytes} 字节");

            return (res.Take(result.ReceivedBytes).ToArray(),
                    (IPEndPoint)result.RemoteEndPoint
                    , local);
        }
    }
}
