using System;

namespace STUN.Messages.StunAttributeValues
{
	/// <summary>
	/// 无法理解的属性
	/// </summary>
	public class UselessStunAttributeValue : IStunAttributeValue
	{
		public int WriteTo(Span<byte> buffer)
		{
			throw new NotSupportedException();
		}

		public bool TryParse(ReadOnlySpan<byte> buffer)
		{
			return true;
		}
	}
}
