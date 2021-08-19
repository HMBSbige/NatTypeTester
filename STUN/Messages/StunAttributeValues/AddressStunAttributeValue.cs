using Microsoft;
using STUN.Enums;
using System;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace STUN.Messages.StunAttributeValues
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5389#section-15.1
	/// </summary>
	public abstract class AddressStunAttributeValue : IStunAttributeValue
	{
		public IpFamily Family { get; set; }

		public ushort Port { get; set; }

		public IPAddress? Address { get; set; }

		public virtual int WriteTo(Span<byte> buffer)
		{
			Verify.Operation(Address is not null, @"You should set Address info!");

			Requires.Range(buffer.Length >= 4 + 4, nameof(buffer));

			buffer[0] = 0;
			buffer[1] = (byte)Family;
			BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], Port);
			Requires.Range(Address.TryWriteBytes(buffer[4..], out var bytesWritten), nameof(buffer));

			return 4 + bytesWritten;
		}

		public virtual bool TryParse(ReadOnlySpan<byte> buffer)
		{
			var length = 4;

			if (buffer.Length < length)
			{
				return false;
			}

			Family = (IpFamily)buffer[1];

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

			if (buffer.Length != length)
			{
				return false;
			}

			Port = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..]);

			Address = new IPAddress(buffer[4..]);

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
