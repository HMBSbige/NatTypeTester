using System.Net;
using System.Threading.Tasks;

namespace STUN.DnsClients
{
	public interface IDnsQuery
	{
		public Task<IPAddress> QueryAsync(string host);
		public IPAddress Query(string host);
	}
}
