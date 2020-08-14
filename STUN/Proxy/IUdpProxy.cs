using System;
using System.Net;
using System.Threading.Tasks;

namespace STUN.Proxy
{
    public interface IUdpProxy
    {
        TimeSpan Timeout { get; set; }
        IPEndPoint LocalEndPoint { get; }
        Task ConnectAsync(IPEndPoint local, IPEndPoint remote);
        Task<(byte[], IPEndPoint, IPAddress)> RecieveAsync(byte[] bytes, IPEndPoint remote, EndPoint receive);
        Task DisconnectAsync();
    }
}