using STUN.Interfaces;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace STUN.Client
{
    public class DefaultDnsQuery : IDnsQuery
    {
        public async Task<IPAddress> QueryAsync(string host)
        {
            try
            {
                var ip = IsIPAddress(host);
                if (ip != null)
                {
                    return ip;
                }
                var res = await Dns.GetHostAddressesAsync(host);
                return res.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        public IPAddress Query(string host)
        {
            try
            {
                var ip = IsIPAddress(host);
                if (ip != null)
                {
                    return ip;
                }
                var res = Dns.GetHostAddresses(host);
                return res.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static IPAddress IsIPAddress(string host)
        {
            if (host != null && IPAddress.TryParse(host, out var ip))
            {
                return ip;
            }
            return null;
        }
    }
}
