using System;

namespace STUN.Client.Interfaces
{
    public interface IStunClient : IDisposable
    {
        public IStunResult Query();
        public IStunResult QueryAsync();
    }
}
