using STUN.Enums;
using STUN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace STUN.Message.Attributes
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5389#section-15.1
	/// </summary>
	public abstract class AddressAttribute : IAttribute
	{
		public virtual IEnumerable<byte> Bytes
		{
			get
			{
				if (Address is null)
				{
					return Array.Empty<byte>();
				}
				var res = new List<byte> { 0, (byte)Family };
				res.AddRange(Port.ToBe());
				res.AddRange(Address.GetAddressBytes());
				return res;
			}
		}

		public IpFamily Family { get; set; }

		public ushort Port { get; set; }

		public IPAddress? Address { get; set; }

		public virtual bool TryParse(byte[] bytes)
		{
			var length = 4;

			if (bytes.Length < length)
			{
				return false;
			}

			Family = (IpFamily)bytes[1];

			switch (Family)
			{
				case IpFamily.IPv4:
					length += 4;
					break;
				case IpFamily.IPv6:
					length += 16;
					break;
				default:
					return false;
			}

			if (bytes.Length != length)
			{
				return false;
			}

			Port = BitUtils.FromBe(bytes[2], bytes[3]);

			Address = new IPAddress(bytes.Skip(4).ToArray());

			return true;
		}

		public override string? ToString()
		{
			return Address?.AddressFamily switch
			{
				AddressFamily.InterNetwork => $@"{Address}:{Port}",
				AddressFamily.InterNetworkV6 => $@"[{Address}]:{Port}",
				_ => base.ToString()
			};
		}
	}
}
