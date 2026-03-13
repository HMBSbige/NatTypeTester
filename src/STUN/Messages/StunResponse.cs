using System.Net;

namespace STUN.Messages;

/// <summary>
/// Represents a STUN response containing the parsed message and the remote and local endpoints involved in the exchange.
/// </summary>
/// <param name="Message">The parsed STUN response message.</param>
/// <param name="Remote">The remote endpoint that sent the response.</param>
/// <param name="Local">The local endpoint that received the response.</param>
public record StunResponse(StunMessage5389 Message, IPEndPoint Remote, IPEndPoint Local);
