using Microsoft;
using STUN.Enums;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;

namespace STUN.Messages;

/// <summary>
/// https://tools.ietf.org/html/rfc5389#section-6
/// </summary>
public class StunMessage5389
{
	#region Header

	private const int SizeOfMessageType = sizeof(StunMessageType);
	private const int SizeOfLength = sizeof(ushort);
	private const int SizeOfMagicCookie = sizeof(uint);
	private const int SizeOfTransactionId = 12;
	public const int HeaderLength = SizeOfMessageType + SizeOfLength + SizeOfMagicCookie + SizeOfTransactionId;

	public StunMessageType StunMessageType { get; set; }

	public uint MagicCookie { get; set; }

	public byte[] TransactionId { get; }

	#endregion

	public IEnumerable<StunAttribute> Attributes { get; set; }

	public ushort MessageLength => (ushort)Attributes.Sum(x => x.RealLength);
	public int Length => HeaderLength + MessageLength;

	public StunMessage5389()
	{
		Attributes = Array.Empty<StunAttribute>();
		StunMessageType = StunMessageType.BindingRequest;
		MagicCookie = 0x2112A442;
		TransactionId = new byte[SizeOfTransactionId];
		RandomNumberGenerator.Fill(TransactionId);
	}

	public int WriteTo(Span<byte> buffer)
	{
		ushort messageLength = MessageLength;
		int length = Length;
		Requires.Range(buffer.Length >= length, nameof(buffer));

		BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)StunMessageType);
		BinaryPrimitives.WriteUInt16BigEndian(buffer[SizeOfMessageType..], messageLength);
		BinaryPrimitives.WriteUInt32BigEndian(buffer[(SizeOfMessageType + SizeOfLength)..], MagicCookie);
		TransactionId.CopyTo(buffer[(SizeOfMessageType + SizeOfLength + SizeOfMagicCookie)..]);

		buffer = buffer[HeaderLength..];
		foreach (StunAttribute attribute in Attributes)
		{
			int outLength = attribute.WriteTo(buffer);
			buffer = buffer[outLength..];
		}

		return length;
	}

	public bool TryParse(ReadOnlyMemory<byte> buffer)
	{
		ReadOnlySequence<byte> sequence = new(buffer);
		return TryParse(ref sequence);
	}

	public bool TryParse(ref ReadOnlySequence<byte> sequence)
	{
		if (sequence.Length < HeaderLength)
		{
			return false; // Check length
		}

		SequenceReader<byte> reader = new(sequence);

		if (!reader.TryReadBigEndian(out short typeValue))
		{
			throw Assumes.NotReachable();
		}

		StunMessageType type = (StunMessageType)(ushort)(typeValue & 0b0011_1111_1111_1111);

		if (!Enum.IsDefined(type))
		{
			return false;
		}

		StunMessageType = type;

		if (!reader.TryReadBigEndian(out short lengthValue))
		{
			throw Assumes.NotReachable();
		}

		ushort length = (ushort)lengthValue;

		if (sequence.Length - HeaderLength < length)
		{
			return false; // Check length
		}

		if (!reader.TryReadBigEndian(out int magicCookie))
		{
			throw Assumes.NotReachable();
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

			while (attributeBuffer.Length > default(int))
			{
				StunAttribute attribute = new();
				int offset = attribute.TryParse(attributeBuffer, magicCookieAndTransactionId);
				if (offset <= default(int))
				{
					Debug.WriteLine($@"[Warning] Ignore wrong attribute: {Convert.ToHexString(attributeBuffer)}");
					break;
				}

				list.Add(attribute);
				attributeBuffer = attributeBuffer[offset..];
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

	public bool IsSameTransaction(StunMessage5389 other)
	{
		return MagicCookie == other.MagicCookie && TransactionId.AsSpan().SequenceEqual(other.TransactionId);
	}
}
