using STUN.Messages;
using System.Net;

namespace STUN.Client;

public sealed class StunDiscoveryAction
{
	public required StunMessage5389 Message { get; init; }
	public required IPEndPoint SendTo { get; init; }
}
