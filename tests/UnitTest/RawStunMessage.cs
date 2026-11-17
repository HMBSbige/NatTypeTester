using STUN.Enums;
using STUN.Messages;
using System.Buffers.Binary;

namespace UnitTest;

/// <summary>
/// Raw message bytes for tests. Assembles Binding Success Responses byte by byte and parses them with <see cref="StunMessage5389.TryParse(ReadOnlyMemory{byte})"/>,
/// so tests can feed attribute values that the library itself never encodes.
/// </summary>
public static class RawStunMessage
{
	/// <summary>
	/// XOR-MAPPED-ADDRESS value of the RFC 5769 §2.2 Sample IPv4 Response: 192.0.2.1:32853.
	/// </summary>
	public const string Rfc5769XorMappedAddress = "0001A147E112A643";

	public const string UnknownFamily = "0003A147E112A643";

	public const string IPv4FamilyWithIPv6Length = "0001A1470113A9FAA5D3F179BC25F4B5BED2B9D9";

	public const string IPv6FamilyWithIPv4Length = "0002A147E112A643";

	/// <summary>
	/// SOFTWARE value "hello", whose 5 bytes need 3 bytes of padding.
	/// </summary>
	public const string Hello = "68656C6C6F";

	/// <summary>
	/// The RFC 5389 magic cookie followed by the RFC 5769 transaction ID.
	/// </summary>
	public static ReadOnlySpan<byte> MagicCookieAndTransactionId =>
	[
		0x21, 0x12, 0xa4, 0x42,
		0xb7, 0xe7, 0xa7, 0x01,
		0xbc, 0x34, 0xd6, 0x86,
		0xfa, 0x87, 0xdf, 0xae
	];

	/// <summary>
	/// A header-sized run of 0xFF bytes. Its message type 0x3FFF is undefined, so it never parses as a STUN message.
	/// </summary>
	public static ReadOnlyMemory<byte> NotStun { get; } = Enumerable.Repeat(byte.MaxValue, StunMessage5389.HeaderLength).ToArray();

	public static StunMessage5389 Parse(params ReadOnlySpan<(AttributeType Type, string Value)> attributes)
	{
		byte[] buffer = Build(attributes);

		StunMessage5389 message = new();

		if (!message.TryParse(buffer) || message.Attributes.Count() != attributes.Length)
		{
			throw new InvalidOperationException(@"The raw STUN message is not framed correctly.");
		}

		return message;
	}

	public static byte[] Build(params ReadOnlySpan<(AttributeType Type, string Value)> attributes)
	{
		List<byte> body = [];

		foreach ((AttributeType type, string value) in attributes)
		{
			byte[] valueBytes = Convert.FromHexString(value);
			body.AddRange([.. BigEndian((ushort)type), .. BigEndian((ushort)valueBytes.Length), .. valueBytes]);
			body.AddRange(new byte[(4 - valueBytes.Length % 4) % 4]);
		}

		return
		[
			0x01, 0x01, // Binding Success Response
			.. BigEndian((ushort)body.Count),
			.. MagicCookieAndTransactionId,
			.. body
		];
	}

	/// <summary>
	/// Builds a Binding Success Response whose only attribute, SOFTWARE <see cref="Hello"/>, lacks the 3 padding bytes that RFC 5389 §15 requires.
	/// The header length of 9 counts only the bytes present, so it is not a multiple of 4 either (RFC 5389 §6).
	/// </summary>
	public static byte[] BuildWithUnpaddedAttribute()
	{
		byte[] padded = Build((AttributeType.Software, Hello));
		byte[] unpadded = padded.AsSpan(0, padded.Length - 3).ToArray();
		BinaryPrimitives.WriteUInt16BigEndian(unpadded.AsSpan(2), (ushort)(unpadded.Length - StunMessage5389.HeaderLength));
		return unpadded;
	}

	private static byte[] BigEndian(ushort value)
	{
		return [(byte)(value >> 8), (byte)value];
	}
}
