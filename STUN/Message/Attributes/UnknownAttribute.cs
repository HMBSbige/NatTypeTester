using STUN.Enums;
using STUN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace STUN.Message.Attributes
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5389#section-15.9
	/// </summary>
	public class UnknownAttribute : IAttribute
	{
		public IEnumerable<byte> Bytes
		{
			get
			{
				var res = new List<byte>();
				foreach (var type in Types)
				{
					res.AddRange(Convert.ToUInt16(type).ToBe());
				}
				return res;
			}
		}

		public IEnumerable<AttributeType> Types { get; set; } = Array.Empty<AttributeType>();

		public bool TryParse(byte[] bytes)
		{
			if (bytes.Length < 2 || (bytes.Length & 1) == 1)
			{
				return false;
			}

			var list = new List<AttributeType>();
			for (var i = 0; i < bytes.Length >> 1; ++i)
			{
				var b = bytes.Skip(i << 1).Take(2);
				list.Add((AttributeType)BitUtils.FromBe(b));
			}
			Types = list;

			return true;
		}
	}
}
