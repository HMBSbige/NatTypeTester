using STUN.Messages;
using System.Net;

namespace STUN.Client;

/// <summary>
/// Represents a STUN discovery action to be sent, consisting of a message and a target endpoint.
/// </summary>
public sealed class StunDiscoveryAction
{
	/// <summary>
	/// Gets the STUN message to send.
	/// </summary>
	public required StunMessage5389 Message { get; init; }

	/// <summary>
	/// Gets the remote endpoint to send the message to.
	/// </summary>
	public required IPEndPoint SendTo { get; init; }
}
