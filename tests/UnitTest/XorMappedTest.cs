using STUN.Enums;
using STUN.Messages.StunAttributeValues;
using System.Net;

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

	private static readonly byte[] XorPort = [0xa1, 0x47];
	private static readonly byte[] XorIPv4 = [0xe1, 0x12, 0xa6, 0x43];
	private static readonly byte[] XorIPv6 = [0x01, 0x13, 0xa9, 0xfa, 0xa5, 0xd3, 0xf1, 0x79, 0xbc, 0x25, 0xf4, 0xb5, 0xbe, 0xd2, 0xb9, 0xd9];

	private const ushort Port = 32853;
	private readonly IPAddress _ipv4 = IPAddress.Parse(@"192.0.2.1");
	private readonly IPAddress _ipv6 = IPAddress.Parse(@"2001:db8:1234:5678:11:2233:4455:6677");

	private readonly byte[] _ipv4Response = ((byte[])[0x00, (byte)IpFamily.IPv4]).Concat(XorPort).Concat(XorIPv4).ToArray();
	private readonly byte[] _ipv6Response = ((byte[])[0x00, (byte)IpFamily.IPv6]).Concat(XorPort).Concat(XorIPv6).ToArray();

	/// <summary>
	/// https://datatracker.ietf.org/doc/html/rfc5769.html
	/// </summary>
	[Test]
	public async Task TestXorMapped()
	{
		XorMappedAddressStunAttributeValue t = new(MagicCookieAndTransactionId)
		{
			Port = Port,
			Family = IpFamily.IPv4,
			Address = _ipv4
		};
		byte[] temp = new byte[64];

		int length4 = t.WriteTo(temp);
		await Assert.That(length4).IsNotEqualTo(0);
		await Assert.That(temp.AsSpan(0, length4).SequenceEqual(_ipv4Response)).IsTrue();

		t = new XorMappedAddressStunAttributeValue(MagicCookieAndTransactionId);
		await Assert.That(t.TryParse(_ipv4Response)).IsTrue();
		await Assert.That(t.Port).IsEqualTo(Port);
		await Assert.That(t.Family).IsEqualTo(IpFamily.IPv4);
		await Assert.That(t.Address).IsEqualTo(_ipv4);

		t = new XorMappedAddressStunAttributeValue(MagicCookieAndTransactionId);
		await Assert.That(t.TryParse(_ipv6Response)).IsTrue();
		await Assert.That(t.Port).IsEqualTo(Port);
		await Assert.That(t.Family).IsEqualTo(IpFamily.IPv6);
		await Assert.That(t.Address).IsEqualTo(_ipv6);

		int length6 = t.WriteTo(temp);
		await Assert.That(length6).IsNotEqualTo(0);
		await Assert.That(temp.AsSpan(0, length6).SequenceEqual(_ipv6Response)).IsTrue();
	}
}
