using STUN.Client;
using STUN.Enums;
using STUN.Messages.StunAttributeValues;
using System.Net;

namespace UnitTest;

/// <summary>
/// Where a <see cref="StunDiscoveryAction"/> sends its request, and which CHANGE-REQUEST flags the request sets.
/// </summary>
internal sealed record DiscoveryRequest(IPEndPoint SendTo, bool ChangeIp = false, bool ChangePort = false)
{
	/// <returns>The request that <paramref name="action"/> sends, or <see langword="null"/> when there is no action because the discovery has ended.</returns>
	public static DiscoveryRequest? From(StunDiscoveryAction? action)
	{
		if (action is null)
		{
			return null;
		}

		return action.Message.Attributes.SingleOrDefault(attribute => attribute.Type is AttributeType.ChangeRequest) switch
		{
			null => new DiscoveryRequest(action.SendTo),
			{ Value: ChangeRequestStunAttributeValue value } => new DiscoveryRequest(action.SendTo, value.ChangeIp, value.ChangePort),
			_ => throw new InvalidOperationException(@"The CHANGE-REQUEST attribute does not hold a CHANGE-REQUEST value.")
		};
	}
}
