using STUN.Messages;
using STUN.Utils;

namespace UnitTest;

public class StunAttributeTest
{
	[Test]
	public async Task TryParseRejectsUnpaddedAttributeAsync()
	{
		// SOFTWARE "hello": the 5-byte value lacks its 3 bytes of padding (RFC 5389 §15)
		byte[] buffer = Convert.FromHexString(@"80220005" + RawStunMessage.Hello);

		await Assert.That(new StunAttribute().TryParse(buffer, RawStunMessage.MagicCookieAndTransactionId)).IsEqualTo(0);
	}

	/// <summary>
	/// RFC 5780 §7.2: the "change IP" flag is bit A (0x4) and the "change port" flag is bit B (0x2) of the last byte.
	/// </summary>
	[Test]
	[Arguments(true, false, "00000004")]
	[Arguments(false, true, "00000002")]
	[Arguments(true, true, "00000006")]
	public async Task BuildChangeRequestEncodesFlagsAsync(bool changeIp, bool changePort, string value)
	{
		byte[] buffer = new byte[8];
		int length = AttributeExtensions.BuildChangeRequest(changeIp, changePort).WriteTo(buffer);

		// TYPE 0x0003, LENGTH 4, VALUE
		await Assert.That(Convert.ToHexString(buffer, 0, length)).IsEqualTo(@"00030004" + value);
	}
}
