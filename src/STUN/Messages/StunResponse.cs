using System.Net;

namespace STUN.Messages;

public record StunResponse(StunMessage5389 Message, IPEndPoint Remote, IPEndPoint Local);
