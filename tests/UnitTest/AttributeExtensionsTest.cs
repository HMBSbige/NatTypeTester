using STUN.Enums;
using STUN.Messages;
using STUN.Utils;
using System.Net;

namespace UnitTest;

public class AttributeExtensionsTest
{
	private const string MappedAddressValue = "000104D2C6336401";
	private static readonly IPEndPoint MappedEndPoint = IPEndPoint.Parse(@"198.51.100.1:1234");

	private const string ChangedAddressValue = "00010D97CB007101";
	private static readonly IPEndPoint ChangedEndPoint = IPEndPoint.Parse(@"203.0.113.1:3479");

	[Test]
	[Arguments(RawStunMessage.UnknownFamily)]
	[Arguments(RawStunMessage.IPv4FamilyWithIPv6Length)]
	[Arguments(RawStunMessage.IPv6FamilyWithIPv4Length)]
	public async Task MalformedAddressesAreIgnored(string malformed)
	{
		StunMessage5389 message = RawStunMessage.Parse
		(
			(AttributeType.XorMappedAddress, malformed),
			(AttributeType.MappedAddress, malformed),
			(AttributeType.OtherAddress, malformed),
			(AttributeType.ChangedAddress, malformed)
		);

		using (Assert.Multiple())
		{
			await Assert.That(message.GetXorMappedAddressAttribute()).IsNull();
			await Assert.That(message.GetMappedAddressAttribute()).IsNull();
			await Assert.That(message.GetOtherAddressAttribute()).IsNull();
			await Assert.That(message.GetChangedAddressAttribute()).IsNull();
		}
	}

	[Test]
	[Arguments(RawStunMessage.UnknownFamily)]
	[Arguments(RawStunMessage.IPv4FamilyWithIPv6Length)]
	[Arguments(RawStunMessage.IPv6FamilyWithIPv4Length)]
	public async Task MalformedAddressesFallBack(string malformed)
	{
		StunMessage5389 message = RawStunMessage.Parse
		(
			(AttributeType.XorMappedAddress, malformed),
			(AttributeType.MappedAddress, MappedAddressValue),
			(AttributeType.OtherAddress, malformed),
			(AttributeType.ChangedAddress, ChangedAddressValue)
		);

		using (Assert.Multiple())
		{
			await Assert.That(message.GetXorMappedAddressAttribute()).IsEqualTo(MappedEndPoint);
			await Assert.That(message.GetOtherAddressAttribute()).IsEqualTo(ChangedEndPoint);
		}
	}
}
