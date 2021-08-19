using Microsoft;
using STUN.Enums;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;

namespace STUN.Messages
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5389#section-6
	/// </summary>
	public class StunMessage5389
	{
		#region Header

		public StunMessageType StunMessageType { get; set; }

		public uint MagicCookie { get; set; }

		public byte[] TransactionId { get; }

		#endregion

		public IEnumerable<StunAttribute> Attributes { get; set; }

		public StunMessage5389()
		{
			Attributes = Array.Empty<StunAttribute>();
			StunMessageType = StunMessageType.BindingRequest;
			MagicCookie = 0x2112A442;
			TransactionId = new byte[12];
			RandomNumberGenerator.Fill(TransactionId);
		}

		public int WriteTo(Span<byte> buffer)
		{
			var messageLength = Attributes.Aggregate<StunAttribute, ushort>(0, (current, attribute) => (ushort)(current + attribute.RealLength));
			var length = 20 + messageLength;
			Requires.Range(buffer.Length >= length, nameof(buffer));

			BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)StunMessageType);
			BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], messageLength);
			BinaryPrimitives.WriteUInt32BigEndian(buffer[4..], MagicCookie);
			TransactionId.CopyTo(buffer[8..]);

			buffer = buffer[20..];
			foreach (var attribute in Attributes)
			{
				var outLength = attribute.WriteTo(buffer);
				buffer = buffer[outLength..];
			}

			return length;
		}

		public bool TryParse(ReadOnlySpan<byte> buffer)
		{
			if (buffer.Length < 20)
			{
				return false; // Check length
			}

			Span<byte> tempSpan = stackalloc byte[2];

			tempSpan[0] = (byte)(buffer[0] & 0b0011_1111);
			tempSpan[1] = buffer[1];
			var type = (StunMessageType)BinaryPrimitives.ReadUInt16BigEndian(tempSpan);

			if (!Enum.IsDefined(typeof(StunMessageType), type))
			{
				return false;
			}

			StunMessageType = type;

			var length = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..]);

			MagicCookie = BinaryPrimitives.ReadUInt32BigEndian(buffer[4..]);

			buffer.Slice(8, 12).CopyTo(TransactionId);

			if (buffer.Length != length + 20)
			{
				return false; // Check length
			}

			var list = new List<StunAttribute>();

			var attributeBuffer = buffer[20..];
			var magicCookieAndTransactionId = buffer.Slice(4, 16);

			while (attributeBuffer.Length > 0)
			{
				var attribute = new StunAttribute();
				var offset = attribute.TryParse(attributeBuffer, magicCookieAndTransactionId);
				if (offset > 0)
				{
					list.Add(attribute);
					attributeBuffer = attributeBuffer[offset..];
				}
				else
				{
					Debug.WriteLine($@"[Warning] Ignore wrong attribute: {Convert.ToHexString(attributeBuffer)}");
					break;
				}
			}

			Attributes = list;

			return true;
		}

		public bool IsSameTransaction(StunMessage5389 other)
		{
			return MagicCookie == other.MagicCookie && TransactionId.AsSpan().SequenceEqual(other.TransactionId);
		}
	}
}
