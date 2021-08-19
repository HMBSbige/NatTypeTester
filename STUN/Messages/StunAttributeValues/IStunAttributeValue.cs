using System;

namespace STUN.Messages.StunAttributeValues
{
	public interface IStunAttributeValue
	{
		int WriteTo(Span<byte> buffer);

		bool TryParse(ReadOnlySpan<byte> buffer);
	}
}
