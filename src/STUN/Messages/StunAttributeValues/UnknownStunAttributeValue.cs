using STUN.Enums;
using System.Buffers.Binary;

namespace STUN.Messages.StunAttributeValues;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5389#section-15.9
/// </summary>
public class UnknownStunAttributeValue : IStunAttributeValue
{
	/// <summary>
	/// Gets the list of unrecognized attribute types reported by the server.
	/// </summary>
	public List<AttributeType> Types { get; } = new();

	/// <summary>
	/// Serializes this UNKNOWN-ATTRIBUTES value into the specified buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer.</param>
	/// <returns>The number of bytes written.</returns>
	public int WriteTo(Span<byte> buffer)
	{
		int size = Types.Count << 1;
		ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, size, nameof(buffer));

		foreach (AttributeType attributeType in Types)
		{
			BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)attributeType);
			buffer = buffer[sizeof(ushort)..];
		}

		return size;
	}

	/// <summary>
	/// Attempts to parse an UNKNOWN-ATTRIBUTES value from the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw attribute value bytes.</param>
	/// <returns><see langword="true"/> if the value was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public bool TryParse(ReadOnlySpan<byte> buffer)
	{
		if (buffer.Length < 2 || (buffer.Length & 1) == 1)
		{
			return false;
		}

		Types.Clear();
		while (!buffer.IsEmpty)
		{
			ushort type = BinaryPrimitives.ReadUInt16BigEndian(buffer);
			Types.Add((AttributeType)type);
			buffer = buffer[sizeof(ushort)..];
		}

		return true;
	}
}
