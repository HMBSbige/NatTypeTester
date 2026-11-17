using STUN.Enums;
using STUN.Messages;
using System.Buffers.Binary;

namespace UnitTest;

public class StunMessage5389Test
{
	[Test]
	public async Task TryParseRejectsUnpaddedLastAttributeAsync()
	{
		byte[] buffer = RawStunMessage.BuildWithUnpaddedAttribute();

		await Assert.That(new StunMessage5389().TryParse(buffer)).IsFalse();
	}

	[Test]
	[Arguments(1)]
	[Arguments(2)]
	[Arguments(3)]
	public async Task TryParseRejectsLengthNotMultipleOfFourAsync(int extraBytes)
	{
		// RFC 5389 §6: attributes are padded to a multiple of 4 bytes, so the 2 low bits of the length are always 0
		byte[] buffer = [.. RawStunMessage.Build((AttributeType.XorMappedAddress, RawStunMessage.Rfc5769XorMappedAddress)), .. new byte[extraBytes]];
		BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), (ushort)(buffer.Length - StunMessage5389.HeaderLength));

		await Assert.That(new StunMessage5389().TryParse(buffer)).IsFalse();
	}

	[Test]
	[Arguments(9)]
	[Arguments(12)]
	[Arguments(ushort.MaxValue)]
	public async Task TryParseRejectsAttributeOverrunningMessageAsync(int attributeLength)
	{
		// RFC 5389 §15: each attribute and its padding must lie within the message. This enlarges the length of the second attribute, XOR-MAPPED-ADDRESS
		byte[] buffer = RawStunMessage.Build((AttributeType.Software, RawStunMessage.Hello), (AttributeType.XorMappedAddress, RawStunMessage.Rfc5769XorMappedAddress));
		int secondAttribute = StunMessage5389.HeaderLength + 4 + 8;
		BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(secondAttribute + 2), (ushort)attributeLength);

		await Assert.That(new StunMessage5389().TryParse(buffer)).IsFalse();
	}
}
