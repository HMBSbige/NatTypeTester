using System.Net;

namespace STUN.Interfaces
{
    public interface IStunResult
    {
        public IPEndPoint PublicEndPoint { get; }
    }
}
