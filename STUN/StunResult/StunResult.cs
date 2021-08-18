using System.Net;

namespace STUN.StunResult
{
	public abstract class StunResult
	{
		public IPEndPoint? PublicEndPoint { get; set; }
		public IPEndPoint? LocalEndPoint { get; set; }
	}
}
