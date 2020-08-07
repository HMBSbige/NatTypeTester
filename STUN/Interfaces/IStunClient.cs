using System;
using System.Threading.Tasks;

namespace STUN.Interfaces
{
    public interface IStunClient : IDisposable
    {
        public IStunResult Query();
        public Task<IStunResult> QueryAsync();
    }
}
