using System.Buffers.Binary;
using System.Net;

namespace STUN.Messages.StunAttributeValues;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5389#section-15.2
/// </summary>
public class XorMappedAddressStunAttributeValue : AddressStunAttributeValue
{
	private readonly byte[] _magicCookieAndTransactionId;

	/// <summary>
	/// Initializes a new instance of the <see cref="XorMappedAddressStunAttributeValue"/> class
	/// with the magic cookie and transaction ID used for XOR obfuscation.
	/// </summary>
	/// <param name="magicCookieAndTransactionId">A 16-byte span containing the magic cookie (4 bytes) followed by the transaction ID (12 bytes).</param>
	public XorMappedAddressStunAttributeValue(ReadOnlySpan<byte> magicCookieAndTransactionId)
	{
		ArgumentOutOfRangeException.ThrowIfNotEqual(magicCookieAndTransactionId.Length, 16, nameof(magicCookieAndTransactionId));
		_magicCookieAndTransactionId = magicCookieAndTransactionId.ToArray();
	}

	/// <summary>
	/// Serializes this XOR-MAPPED-ADDRESS attribute value into the specified buffer, applying XOR obfuscation to the port and address.
	/// </summary>
	/// <param name="buffer">The destination buffer.</param>
	/// <returns>The number of bytes written.</returns>
	public override int WriteTo(Span<byte> buffer)
	{
		IPAddress address = Address ?? throw new InvalidOperationException(@"You should set Address info!");

		ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, 4 + 4, nameof(buffer));

		buffer[0] = 0;
		buffer[1] = (byte)Family;
		BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], Xor(Port));
		if (!Xor(address).TryWriteBytes(buffer[4..], out int bytesWritten))
		{
			throw new ArgumentException(@"Buffer is too small.", nameof(buffer));
		}

		return 4 + bytesWritten;
	}

	/// <summary>
	/// Attempts to parse an XOR-MAPPED-ADDRESS attribute value from the specified buffer, reversing the XOR obfuscation on the port and address.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw attribute value bytes.</param>
	/// <returns><see langword="true"/> if the value was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public override bool TryParse(ReadOnlySpan<byte> buffer)
	{
		if (!base.TryParse(buffer))
		{
			return false;
		}

		IPAddress address = Address ?? throw new InvalidOperationException(@"Address is missing after parsing.");

		Port = Xor(Port);

		Address = Xor(address);

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
		if (!address.TryWriteBytes(b, out int bytesWritten))
		{
			throw new InvalidOperationException(@"Unable to encode address.");
		}

		for (int i = 0; i < bytesWritten; ++i)
		{
			b[i] ^= _magicCookieAndTransactionId[i];
		}

		return new IPAddress(b[..bytesWritten]);
	}
}
