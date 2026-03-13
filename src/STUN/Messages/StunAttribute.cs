using STUN.Enums;
using STUN.Messages.StunAttributeValues;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace STUN.Messages;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5389#section-15
/// </summary>
public class StunAttribute
{
	/*
        Length 是大端
        必须4字节对齐
        对齐的字节可以是任意值
         0                   1                   2                   3
         0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |         Type                  |            Length             |
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        |                         Value (variable)                ....
        +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
     */

	/// <summary>
	/// Gets or sets the STUN attribute type.
	/// </summary>
	public AttributeType Type { get; set; } = AttributeType.Useless;

	/// <summary>
	/// Gets or sets the length of the attribute value in bytes.
	/// </summary>
	public ushort Length { get; set; }

	/// <summary>
	/// Gets the total byte length of this attribute including type, length, value, and padding.
	/// </summary>
	public ushort RealLength => (ushort)(Type == AttributeType.Useless ? 0 : 4 + Length + (4 - Length % 4) % 4);

	/// <summary>
	/// Gets or sets the parsed attribute value.
	/// </summary>
	public IStunAttributeValue Value { get; set; } = new UselessStunAttributeValue();

	/// <summary>
	/// Serializes this attribute into the specified buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer to write the attribute bytes into.</param>
	/// <returns>The total number of bytes written, including padding.</returns>
	public int WriteTo(Span<byte> buffer)
	{
		int length = 4 + Length;
		int n = (4 - length % 4) % 4; // 填充的字节数
		int totalLength = length + n;

		ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, totalLength, nameof(buffer));

		BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)Type);
		BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], Length);
		int valueLength = Value.WriteTo(buffer[4..]);

		if (valueLength != Length)
		{
			throw new InvalidOperationException(@"Attribute value length does not match header length.");
		}

		RandomNumberGenerator.Fill(buffer.Slice(length, n));

		return totalLength;
	}

	/// <summary>
	/// Attempts to parse a STUN attribute from the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw attribute bytes.</param>
	/// <param name="magicCookieAndTransactionId">The magic cookie and transaction ID bytes used for XOR-based attribute decoding.</param>
	/// <returns>
	/// Parse 成功字节，0 则表示 Parse 失败
	/// </returns>
	public int TryParse(ReadOnlySpan<byte> buffer, ReadOnlySpan<byte> magicCookieAndTransactionId)
	{
		if (buffer.Length < 4)
		{
			return default;
		}

		Type = (AttributeType)BinaryPrimitives.ReadUInt16BigEndian(buffer);

		Length = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..]);

		if (buffer.Length < 4 + Length)
		{
			return default;
		}

		ReadOnlySpan<byte> value = buffer.Slice(4, Length);

		IStunAttributeValue t = Type switch
		{
			AttributeType.MappedAddress => new MappedAddressStunAttributeValue(),
			AttributeType.XorMappedAddress => new XorMappedAddressStunAttributeValue(magicCookieAndTransactionId),
			AttributeType.ResponseAddress => new ResponseAddressStunAttributeValue(),
			AttributeType.ChangeRequest => new ChangeRequestStunAttributeValue(),
			AttributeType.SourceAddress => new SourceAddressStunAttributeValue(),
			AttributeType.ChangedAddress => new ChangedAddressStunAttributeValue(),
			AttributeType.OtherAddress => new OtherAddressStunAttributeValue(),
			AttributeType.ReflectedFrom => new ReflectedFromStunAttributeValue(),
			AttributeType.ErrorCode => new ErrorCodeStunAttributeValue(),
			_ => new UselessStunAttributeValue()
		};
		if (t.TryParse(value))
		{
			Value = t;
		}

		return 4 + Length + (4 - Length % 4) % 4; // 对齐
	}
}
