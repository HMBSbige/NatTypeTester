using STUN.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Proxy
{
    public class NoneUdpProxy : IUdpProxy
    {
        public TimeSpan Timeout
        {
            get => TimeSpan.FromMilliseconds(UdpClient.Client.ReceiveTimeout);
            set => UdpClient.Client.ReceiveTimeout = Convert.ToInt32(value.TotalMilliseconds);
        }

        public IPEndPoint LocalEndPoint => (IPEndPoint)UdpClient.Client.LocalEndPoint;

        protected UdpClient UdpClient;

        public NoneUdpProxy(IPEndPoint local)
        {
            UdpClient = local == null ? new UdpClient() : new UdpClient(local);
        }

        public Task ConnectAsync(CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            UdpClient.Close();
            return Task.CompletedTask;
        }

        public async Task<(byte[], IPEndPoint, IPAddress)> ReceiveAsync(byte[] bytes, IPEndPoint remote, EndPoint receive, CancellationToken token = default)
        {
            var localEndPoint = (IPEndPoint)UdpClient.Client.LocalEndPoint;

            Debug.WriteLine($@"{localEndPoint} => {remote} {bytes.Length} 字节");

            await UdpClient.SendAsync(bytes, bytes.Length, remote);

            var res = new byte[ushort.MaxValue];
            var flag = SocketFlags.None;

            var length = UdpClient.Client.ReceiveMessageFrom(res, 0, res.Length, ref flag, ref receive, out var ipPacketInformation);

            var local = ipPacketInformation.Address;

            Debug.WriteLine($@"{(IPEndPoint)receive} => {local} {length} 字节");

            return (res.Take(length).ToArray(),
                    (IPEndPoint)receive
                    , local);
        }

        public void Dispose()
        {
            UdpClient?.Dispose();
        }
    }
}
