using STUN.Enums;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;

namespace STUN.Messages;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5389#section-6
/// </summary>
public class StunMessage5389
{
	#region Header

	private const int SizeOfMessageType = sizeof(StunMessageType);
	private const int SizeOfLength = sizeof(ushort);
	private const int SizeOfMagicCookie = sizeof(uint);
	private const int SizeOfTransactionId = 12;
	/// <summary>
	/// The fixed size of the STUN message header in bytes.
	/// </summary>
	public const int HeaderLength = SizeOfMessageType + SizeOfLength + SizeOfMagicCookie + SizeOfTransactionId;

	/// <summary>
	/// Gets or sets the STUN message type indicating the request or response class and method.
	/// </summary>
	public StunMessageType StunMessageType { get; set; }

	/// <summary>
	/// Gets or sets the magic cookie value, which is fixed at <c>0x2112A442</c> for RFC 5389.
	/// </summary>
	public uint MagicCookie { get; set; }

	/// <summary>
	/// Gets the 96-bit transaction ID that uniquely identifies a STUN transaction.
	/// </summary>
	public byte[] TransactionId { get; }

	#endregion

	/// <summary>
	/// Gets or sets the collection of STUN attributes contained in this message.
	/// </summary>
	public IEnumerable<StunAttribute> Attributes { get; set; }

	/// <summary>
	/// Gets the total byte length of all attributes in this message.
	/// </summary>
	public ushort MessageLength => (ushort)Attributes.Sum(x => x.RealLength);

	/// <summary>
	/// Gets the total byte length of this message including header and all attributes.
	/// </summary>
	public int Length => HeaderLength + MessageLength;

	/// <summary>
	/// Initializes a new <see cref="StunMessage5389"/> with a Binding Request type, the standard magic cookie, and a random transaction ID.
	/// </summary>
	public StunMessage5389()
	{
		Attributes = Array.Empty<StunAttribute>();
		StunMessageType = StunMessageType.BindingRequest;
		MagicCookie = 0x2112A442;
		TransactionId = new byte[SizeOfTransactionId];
		RandomNumberGenerator.Fill(TransactionId);
	}

	/// <summary>
	/// Serializes this STUN message into the specified buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer to write the message bytes into.</param>
	/// <returns>The total number of bytes written.</returns>
	public int WriteTo(Span<byte> buffer)
	{
		ushort messageLength = MessageLength;
		int length = Length;
		ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, length, nameof(buffer));

		BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)StunMessageType);
		BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(SizeOfMessageType), messageLength);
		BinaryPrimitives.WriteUInt32BigEndian(buffer.Slice(SizeOfMessageType + SizeOfLength), MagicCookie);
		TransactionId.CopyTo(buffer.Slice(SizeOfMessageType + SizeOfLength + SizeOfMagicCookie));

		buffer = buffer.Slice(HeaderLength);
		foreach (StunAttribute attribute in Attributes)
		{
			int outLength = attribute.WriteTo(buffer);
			buffer = buffer.Slice(outLength);
		}

		return length;
	}

	/// <summary>
	/// Attempts to parse a STUN message from the specified memory buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw STUN message bytes.</param>
	/// <returns><see langword="true"/> if the message was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public bool TryParse(ReadOnlyMemory<byte> buffer)
	{
		ReadOnlySequence<byte> sequence = new(buffer);
		return TryParse(ref sequence);
	}

	/// <summary>
	/// Attempts to parse a STUN message from the specified byte sequence, advancing the sequence past the consumed bytes on success.
	/// </summary>
	/// <param name="sequence">The byte sequence to parse from. On success, it is advanced past the consumed bytes.</param>
	/// <returns><see langword="true"/> if the message was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public bool TryParse(ref ReadOnlySequence<byte> sequence)
	{
		if (sequence.Length < HeaderLength)
		{
			return false; // Check length
		}

		SequenceReader<byte> reader = new(sequence);

		if (!reader.TryReadBigEndian(out short typeValue))
		{
			throw new UnreachableException();
		}

		StunMessageType type = (StunMessageType)(ushort)(typeValue & 0b0011_1111_1111_1111);

		if (!Enum.IsDefined(type))
		{
			return false;
		}

		StunMessageType = type;

		if (!reader.TryReadBigEndian(out short lengthValue))
		{
			throw new UnreachableException();
		}

		ushort length = (ushort)lengthValue;

		if (sequence.Length - HeaderLength < length)
		{
			return false; // Check length
		}

		if (!reader.TryReadBigEndian(out int magicCookie))
		{
			throw new UnreachableException();
		}

		MagicCookie = (uint)magicCookie;

		reader.UnreadSequence.Slice(0, SizeOfTransactionId).CopyTo(TransactionId);
		reader.Advance(SizeOfTransactionId);

		byte[] tempBuffer = ArrayPool<byte>.Shared.Rent(length + SizeOfMagicCookie + SizeOfTransactionId);
		try
		{
			reader.UnreadSequence.Slice(0, length).CopyTo(tempBuffer);
			reader.Advance(length);
			sequence.Slice(SizeOfMessageType + SizeOfLength, SizeOfMagicCookie + SizeOfTransactionId).CopyTo(tempBuffer.AsSpan(length));

			List<StunAttribute> list = new();

			Span<byte> attributeBuffer = tempBuffer.AsSpan(0, length);
			ReadOnlySpan<byte> magicCookieAndTransactionId = tempBuffer.AsSpan(length, SizeOfMagicCookie + SizeOfTransactionId);

			while (attributeBuffer.Length > 0)
			{
				StunAttribute attribute = new();
				int offset = attribute.TryParse(attributeBuffer, magicCookieAndTransactionId);
				if (offset <= 0)
				{
					Debug.WriteLine($@"[Warning] Ignore wrong attribute: {Convert.ToHexString(attributeBuffer)}");
					break;
				}

				list.Add(attribute);
				attributeBuffer = attributeBuffer.Slice(offset);
			}

			Attributes = list;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(tempBuffer);
		}

		sequence = reader.UnreadSequence;
		return true;
	}

	/// <summary>
	/// Determines whether this message belongs to the same STUN transaction as another message
	/// by comparing their magic cookie and transaction ID.
	/// </summary>
	/// <param name="other">The other STUN message to compare with.</param>
	/// <returns><see langword="true"/> if both messages share the same transaction; otherwise, <see langword="false"/>.</returns>
	public bool IsSameTransaction(StunMessage5389 other)
	{
		return MagicCookie == other.MagicCookie && TransactionId.AsSpan().SequenceEqual(other.TransactionId);
	}
}
