using Microsoft;
using System;
using System.Buffers.Binary;
using System.Net;

namespace STUN.Messages.StunAttributeValues
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5389#section-15.2
	/// </summary>
	public class XorMappedAddressStunAttributeValue : AddressStunAttributeValue
	{
		private readonly byte[] _magicCookieAndTransactionId;

		public XorMappedAddressStunAttributeValue(ReadOnlySpan<byte> magicCookieAndTransactionId)
		{
			Requires.Argument(magicCookieAndTransactionId.Length == 16, nameof(magicCookieAndTransactionId), @"Wrong Transaction ID length");
			_magicCookieAndTransactionId = magicCookieAndTransactionId.ToArray();
		}

		public override int WriteTo(Span<byte> buffer)
		{
			Verify.Operation(Address is not null, @"You should set Address info!");

			Requires.Range(buffer.Length >= 4 + 4, nameof(buffer));

			buffer[0] = 0;
			buffer[1] = (byte)Family;
			BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], Xor(Port));
			Requires.Range(Xor(Address).TryWriteBytes(buffer[4..], out var bytesWritten), nameof(buffer));

			return 4 + bytesWritten;
		}

		public override bool TryParse(ReadOnlySpan<byte> buffer)
		{
			if (!base.TryParse(buffer))
			{
				return false;
			}

			Assumes.NotNull(Address);

			Port = Xor(Port);

			Address = Xor(Address);

			return true;
		}

		private ushort Xor(ushort port)
		{
			Span<byte> span = stackalloc byte[2];
			BinaryPrimitives.WriteUInt16BigEndian(span, port);
			span[0] ^= _magicCookieAndTransactionId[0];
			span[1] ^= _magicCookieAndTransactionId[1];
			return BinaryPrimitives.ReadUInt16BigEndian(span);
		}

		private IPAddress Xor(IPAddress address)
		{
			Span<byte> b = stackalloc byte[16];
			Assumes.True(address.TryWriteBytes(b, out var bytesWritten));

			for (var i = 0; i < bytesWritten; ++i)
			{
				b[i] ^= _magicCookieAndTransactionId[i];
			}

			return new IPAddress(b[..bytesWritten]);
		}
	}
}
