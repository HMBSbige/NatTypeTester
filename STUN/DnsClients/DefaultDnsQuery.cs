using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace STUN.DnsClients
{
	public class DefaultDnsQuery : IDnsQuery
	{
		public async Task<IPAddress?> QueryAsync(string? host)
		{
			try
			{
				if (host is null)
				{
					return null;
				}

				var ip = IsIPAddress(host);
				if (ip is not null)
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

		public IPAddress? Query(string? host)
		{
			try
			{
				if (host is null)
				{
					return null;
				}

				var ip = IsIPAddress(host);
				if (ip is not null)
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

		private static IPAddress? IsIPAddress(string? host)
		{
			if (host is not null && IPAddress.TryParse(host, out var ip))
			{
				return ip;
			}
			return null;
		}
	}
}
