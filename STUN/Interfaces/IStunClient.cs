using System;

namespace STUN.Interfaces
{
    public interface IStunClient : IDisposable
    {
        public IStunResult Query();
        public IStunResult QueryAsync();
    }
}
