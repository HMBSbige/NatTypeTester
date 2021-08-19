using Microsoft;
using System;

namespace STUN.Messages.StunAttributeValues
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5780#section-7.2
	/// </summary>
	public class ChangeRequestStunAttributeValue : IStunAttributeValue
	{
		public bool ChangeIp { get; set; }

		public bool ChangePort { get; set; }

		public int WriteTo(Span<byte> buffer)
		{
			Requires.Range(buffer.Length >= 4, nameof(buffer));

			buffer[0] = buffer[1] = buffer[2] = 0;

			buffer[3] = (byte)(Convert.ToInt32(ChangeIp) << 2 | Convert.ToInt32(ChangePort) << 1);

			return 4;
		}

		public bool TryParse(ReadOnlySpan<byte> buffer)
		{
			if (buffer.Length != 4)
			{
				return false;
			}

			ChangeIp = Convert.ToBoolean(buffer[3] >> 2 & 1);
			ChangePort = Convert.ToBoolean(buffer[3] >> 1 & 1);

			return true;
		}
	}
}
