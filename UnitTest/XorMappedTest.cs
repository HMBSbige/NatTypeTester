using Microsoft.VisualStudio.TestTools.UnitTesting;
using STUN.Enums;
using STUN.Messages.StunAttributeValues;
using System.Net;

namespace UnitTest;

[TestClass]
public class XorMappedTest
{
	private static ReadOnlySpan<byte> MagicCookieAndTransactionId => new byte[]
	{
		0x21, 0x12, 0xa4, 0x42,
		0xb7, 0xe7, 0xa7, 0x01,
		0xbc, 0x34, 0xd6, 0x86,
		0xfa, 0x87, 0xdf, 0xae
	};

	private static readonly byte[] XorPort = { 0xa1, 0x47 };
	private static readonly byte[] XorIPv4 = { 0xe1, 0x12, 0xa6, 0x43 };
	private static readonly byte[] XorIPv6 =
	{
		0x01, 0x13, 0xa9, 0xfa,
		0xa5, 0xd3, 0xf1, 0x79,
		0xbc, 0x25, 0xf4, 0xb5,
		0xbe, 0xd2, 0xb9, 0xd9
	};

	private const ushort Port = 32853;
	private readonly IPAddress IPv4 = IPAddress.Parse(@"192.0.2.1");
	private readonly IPAddress IPv6 = IPAddress.Parse(@"2001:db8:1234:5678:11:2233:4455:6677");

	private readonly byte[] _ipv4Response = new byte[] { 0x00, (byte)IpFamily.IPv4 }.Concat(XorPort).Concat(XorIPv4).ToArray();
	private readonly byte[] _ipv6Response = new byte[] { 0x00, (byte)IpFamily.IPv6 }.Concat(XorPort).Concat(XorIPv6).ToArray();

	/// <summary>
	/// https://datatracker.ietf.org/doc/html/rfc5769.html
	/// </summary>
	[TestMethod]
	public void TestXorMapped()
	{
		XorMappedAddressStunAttributeValue t = new(MagicCookieAndTransactionId)
		{
			Port = Port,
			Family = IpFamily.IPv4,
			Address = IPv4
		};
		Span<byte> temp = stackalloc byte[ushort.MaxValue];

		int length4 = t.WriteTo(temp);
		Assert.AreNotEqual(0, length4);
		Assert.IsTrue(temp[..length4].SequenceEqual(_ipv4Response));

		t = new XorMappedAddressStunAttributeValue(MagicCookieAndTransactionId);
		Assert.IsTrue(t.TryParse(_ipv4Response));
		Assert.AreEqual(t.Port, Port);
		Assert.AreEqual(t.Family, IpFamily.IPv4);
		Assert.AreEqual(t.Address, IPv4);

		t = new XorMappedAddressStunAttributeValue(MagicCookieAndTransactionId);
		Assert.IsTrue(t.TryParse(_ipv6Response));
		Assert.AreEqual(t.Port, Port);
		Assert.AreEqual(t.Family, IpFamily.IPv6);
		Assert.AreEqual(t.Address, IPv6);

		int length6 = t.WriteTo(temp);
		Assert.AreNotEqual(0, length6);
		Assert.IsTrue(temp[..length6].SequenceEqual(_ipv6Response));
	}
}
