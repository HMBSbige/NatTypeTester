using System.Net;

namespace STUN.Messages
{
	public class StunResponse
	{
		public StunMessage5389 Message { get; set; }
		public IPEndPoint Remote { get; set; }
		public IPAddress LocalAddress { get; set; }

		public StunResponse(StunMessage5389 message, IPEndPoint remote, IPAddress localAddress)
		{
			Message = message;
			Remote = remote;
			LocalAddress = localAddress;
		}
	}
}
