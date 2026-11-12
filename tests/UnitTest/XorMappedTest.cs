using STUN.Enums;
using STUN.Messages.StunAttributeValues;
using System.Net;
using TUnit.Assertions.Enums;

namespace UnitTest;

public class XorMappedTest
{
	private static ReadOnlySpan<byte> MagicCookieAndTransactionId =>
	[
		0x21, 0x12, 0xa4, 0x42,
		0xb7, 0xe7, 0xa7, 0x01,
		0xbc, 0x34, 0xd6, 0x86,
		0xfa, 0x87, 0xdf, 0xae
	];

	private const ushort Port = 32853;

	/// <summary>
	/// https://datatracker.ietf.org/doc/html/rfc5769
	/// </summary>
	[Test]
	[Arguments(IpFamily.IPv4, "192.0.2.1", "0001A147E112A643")]
	[Arguments(IpFamily.IPv6, "2001:db8:1234:5678:11:2233:4455:6677", "0002A1470113A9FAA5D3F179BC25F4B5BED2B9D9")]
	public async Task TestXorMapped(IpFamily family, IPAddress address, string encoded)
	{
		byte[] expectedBytes = Convert.FromHexString(encoded);
		XorMappedAddressStunAttributeValue value = new(MagicCookieAndTransactionId)
		{
			Port = Port,
			Family = family,
			Address = address
		};
		byte[] buffer = new byte[expectedBytes.Length];

		int length = value.WriteTo(buffer);
		await Assert.That(length).IsEqualTo(expectedBytes.Length);
		await Assert.That(buffer.AsMemory(0, length)).IsEquivalentTo(expectedBytes, EqualityComparer<byte>.Default, CollectionOrdering.Matching);

		value = new XorMappedAddressStunAttributeValue(MagicCookieAndTransactionId);
		await Assert.That(value.TryParse(expectedBytes)).IsTrue();

		using (Assert.Multiple())
		{
			await Assert.That(value.Port).IsEqualTo(Port);
			await Assert.That(value.Family).IsEqualTo(family);
			await Assert.That(value.Address).IsEqualTo(address);
		}

		buffer.AsSpan().Clear();
		int roundTripLength = value.WriteTo(buffer);
		await Assert.That(roundTripLength).IsEqualTo(expectedBytes.Length);
		await Assert.That(buffer.AsMemory(0, roundTripLength)).IsEquivalentTo(expectedBytes, EqualityComparer<byte>.Default, CollectionOrdering.Matching);
	}
}
