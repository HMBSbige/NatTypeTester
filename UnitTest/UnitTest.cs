using Microsoft.VisualStudio.TestTools.UnitTesting;
using STUN.Client;
using STUN.Enums;
using STUN.Message.Attributes;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace UnitTest
{
    [TestClass]
    public class UnitTest
    {
        private readonly byte[] _magicCookie = { 0x21, 0x12, 0xa4, 0x42 };
        private readonly byte[] _transactionId =
        {
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
        /// https://tools.ietf.org/html/rfc5769.html
        /// </summary>
        [TestMethod]
        public void TestXorMapped()
        {
            var t = new XorMappedAddressAttribute(_magicCookie, _transactionId)
            {
                Port = Port,
                Family = IpFamily.IPv4,
                Address = IPv4
            };
            Assert.IsTrue(_ipv4Response.SequenceEqual(t.Bytes));

            t = new XorMappedAddressAttribute(_magicCookie, _transactionId);
            Assert.IsTrue(t.TryParse(_ipv4Response));
            Assert.AreEqual(t.Port, Port);
            Assert.AreEqual(t.Family, IpFamily.IPv4);
            Assert.AreEqual(t.Address, IPv4);

            t = new XorMappedAddressAttribute(_magicCookie, _transactionId);
            Assert.IsTrue(t.TryParse(_ipv6Response));
            Assert.AreEqual(t.Port, Port);
            Assert.AreEqual(t.Family, IpFamily.IPv6);
            Assert.AreEqual(t.Address, IPv6);

            Assert.IsTrue(_ipv6Response.SequenceEqual(t.Bytes));
        }

        [TestMethod]
        public async Task BindingTest()
        {
            var client = new StunClient5389UDP(@"stun.syncthing.net", 3478, new IPEndPoint(IPAddress.Any, 0));
            var result = await client.BindingTestAsync();

            Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
            Assert.IsNotNull(result.LocalEndPoint);
            Assert.IsNotNull(result.PublicEndPoint);
            Assert.AreNotEqual(result.LocalEndPoint.Address, IPAddress.Any);
        }
    }
}
