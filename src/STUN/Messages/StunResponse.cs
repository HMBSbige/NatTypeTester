using System.Net;

namespace STUN.Messages;

public record StunResponse(StunMessage5389 Message, IPEndPoint Remote, IPEndPoint Local)
{
	public StunMessage5389 Message { get; set; } = Message;
	public IPEndPoint Remote { get; set; } = Remote;
	public IPEndPoint Local { get; set; } = Local;
}
