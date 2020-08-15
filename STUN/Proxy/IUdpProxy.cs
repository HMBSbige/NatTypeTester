using System;
using System.Net;
using System.Threading.Tasks;

namespace STUN.Proxy
{
    public interface IUdpProxy : IDisposable
    {
        TimeSpan Timeout { get; set; }
        IPEndPoint LocalEndPoint { get; }
        Task ConnectAsync();
        Task<(byte[], IPEndPoint, IPAddress)> RecieveAsync(byte[] bytes, IPEndPoint remote, EndPoint receive);
        Task DisconnectAsync();
    }
}