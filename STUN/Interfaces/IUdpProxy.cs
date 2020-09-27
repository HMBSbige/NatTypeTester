using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Interfaces
{
    public interface IUdpProxy : IDisposable
    {
        TimeSpan Timeout { get; set; }
        IPEndPoint LocalEndPoint { get; }
        Task ConnectAsync(CancellationToken token = default);
        Task<(byte[], IPEndPoint, IPAddress)> ReceiveAsync(byte[] bytes, IPEndPoint remote, EndPoint receive, CancellationToken token = default);
        Task DisconnectAsync();
    }
}