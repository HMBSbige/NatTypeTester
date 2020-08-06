using System.Net;

namespace STUN.Client.Interfaces
{
    public interface IStunResult
    {
        public IPEndPoint PublicEndPoint { get; }
    }
}
