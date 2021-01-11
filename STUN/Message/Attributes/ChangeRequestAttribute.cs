using System;
using System.Collections;
using System.Collections.Generic;

namespace STUN.Message.Attributes
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5780#section-7.2
	/// </summary>
	public class ChangeRequestAttribute : IAttribute
	{
		public IEnumerable<byte> Bytes => new byte[] { 0, 0, 0, (byte)(Convert.ToInt32(ChangeIp) << 2 | Convert.ToInt32(ChangePort) << 1) };

		public bool ChangeIp { get; set; }

		public bool ChangePort { get; set; }

		public bool TryParse(byte[] bytes)
		{
			if (bytes.Length != 4)
			{
				return false;
			}

			var bits = new BitArray(bytes);

			ChangeIp = bits[29];
			ChangePort = bits[30];

			return true;
		}
	}
}
