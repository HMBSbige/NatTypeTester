using STUN.Messages;

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
}
