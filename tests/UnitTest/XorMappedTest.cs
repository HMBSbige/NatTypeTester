using STUN.Enums;
using STUN.Messages.StunAttributeValues;
using System.Net;

namespace UnitTest;

public class XorMappedTest
{
	private const ushort Port = 32853;

	/// <summary>
	/// https://datatracker.ietf.org/doc/html/rfc5769
	/// </summary>
	[Test]
	[Arguments(IpFamily.IPv4, "192.0.2.1", "0001A147E112A643")]
	[Arguments(IpFamily.IPv6, "2001:db8:1234:5678:11:2233:4455:6677", "0002A1470113A9FAA5D3F179BC25F4B5BED2B9D9")]
	public async Task TestXorMapped(IpFamily family, IPAddress address, string encoded)
	{
		XorMappedAddressStunAttributeValue value = new(RawStunMessage.MagicCookieAndTransactionId)
		{
			Port = Port,
			Family = family,
			Address = address
		};
		// Room for the longest value, an IPv6 address, so that a wrong length shows up in the comparison
		byte[] buffer = new byte[4 + 16];

		int length = value.WriteTo(buffer);
		await Assert.That(Convert.ToHexString(buffer, 0, length)).IsEqualTo(encoded);

		XorMappedAddressStunAttributeValue parsed = new(RawStunMessage.MagicCookieAndTransactionId);
		await Assert.That(parsed.TryParse(Convert.FromHexString(encoded))).IsTrue();

		using (Assert.Multiple())
		{
			await Assert.That(parsed.Port).IsEqualTo(Port);
			await Assert.That(parsed.Family).IsEqualTo(family);
			await Assert.That(parsed.Address).IsEqualTo(address);
		}
	}
}
