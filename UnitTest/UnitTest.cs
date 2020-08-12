using Microsoft.VisualStudio.TestTools.UnitTesting;
using STUN.Client;
using STUN.Enums;
using STUN.Message.Attributes;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using STUN.Utils;

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
        public void ParseEndpointTest()
        {
            Assert.IsNull(NetUtils.ParseEndpoint(@"1.2.3.4"));
            Assert.IsNull(NetUtils.ParseEndpoint(@"1.2.256.5:80"));
            Assert.AreEqual(NetUtils.ParseEndpoint(@"0.0.0.0:123"), IPEndPoint.Parse(@"0.0.0.0:123"));
            Assert.AreEqual(NetUtils.ParseEndpoint(@"192.168.1.1:2136"), IPEndPoint.Parse(@"192.168.1.1:2136"));
            Assert.AreEqual(NetUtils.ParseEndpoint(@"[2001:db8:1234:5678:11:2233:4455:6677]:32853"), IPEndPoint.Parse(@"[2001:db8:1234:5678:11:2233:4455:6677]:32853"));
            Assert.IsNull(NetUtils.ParseEndpoint(@"2001:db8:1234:5678:11:2233:4455:6677:32853"));
            Assert.IsNull(NetUtils.ParseEndpoint(@"2001:db8:1234:5678:11:2233:4455:6677"));
            Assert.AreEqual(NetUtils.ParseEndpoint(@"[2001:db8:1234:5678:11:2233:4455:6677]:0"), IPEndPoint.Parse(@"[2001:db8:1234:5678:11:2233:4455:6677]:0"));
            Assert.AreEqual(NetUtils.ParseEndpoint(@"[::1]:0"), IPEndPoint.Parse(@"[::1]:0"));
        }

        [TestMethod]
        public async Task BindingTest()
        {
            using var client = new StunClient5389UDP(@"stun.syncthing.net", 3478, new IPEndPoint(IPAddress.Any, 0));
            var result = await client.BindingTestAsync();

            Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
            Assert.IsNotNull(result.LocalEndPoint);
            Assert.IsNotNull(result.PublicEndPoint);
            Assert.IsNotNull(result.OtherEndPoint);
            Assert.AreNotEqual(result.LocalEndPoint.Address, IPAddress.Any);
            Assert.AreEqual(result.MappingBehavior, MappingBehavior.Unknown);
            Assert.AreEqual(result.FilteringBehavior, FilteringBehavior.Unknown);
        }

        [TestMethod]
        public async Task MappingBehaviorTest()
        {
            using var client = new StunClient5389UDP(@"stun.syncthing.net", 3478, new IPEndPoint(IPAddress.Any, 0));
            var result = await client.MappingBehaviorTestAsync();

            Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
            Assert.IsNotNull(result.LocalEndPoint);
            Assert.IsNotNull(result.PublicEndPoint);
            Assert.IsNotNull(result.OtherEndPoint);
            Assert.AreNotEqual(result.LocalEndPoint.Address, IPAddress.Any);
            Assert.IsTrue(result.MappingBehavior == MappingBehavior.Direct
            || result.MappingBehavior == MappingBehavior.EndpointIndependent
            || result.MappingBehavior == MappingBehavior.AddressDependent
            || result.MappingBehavior == MappingBehavior.AddressAndPortDependent
            );
            Assert.AreEqual(result.FilteringBehavior, FilteringBehavior.Unknown);
        }

        [TestMethod]
        public async Task FilteringBehaviorTest()
        {
            using var client = new StunClient5389UDP(@"stun.syncthing.net", 3478, new IPEndPoint(IPAddress.Any, 0));
            var result = await client.FilteringBehaviorTestAsync();

            Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
            Assert.IsNotNull(result.LocalEndPoint);
            Assert.IsNotNull(result.PublicEndPoint);
            Assert.IsNotNull(result.OtherEndPoint);
            Assert.AreNotEqual(result.LocalEndPoint.Address, IPAddress.Any);
            Assert.AreEqual(result.MappingBehavior, MappingBehavior.Unknown);
            Assert.IsTrue(result.FilteringBehavior == FilteringBehavior.EndpointIndependent
            || result.FilteringBehavior == FilteringBehavior.AddressDependent
            || result.FilteringBehavior == FilteringBehavior.AddressAndPortDependent
            );
        }

        [TestMethod]
        public async Task CombiningTest()
        {
            using var client = new StunClient5389UDP(@"stun.syncthing.net", 3478, new IPEndPoint(IPAddress.Any, 0));
            var result = await client.QueryAsync();

            Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
            Assert.IsNotNull(result.LocalEndPoint);
            Assert.IsNotNull(result.PublicEndPoint);
            Assert.IsNotNull(result.OtherEndPoint);
            Assert.AreNotEqual(result.LocalEndPoint.Address, IPAddress.Any);
            Assert.IsTrue(result.MappingBehavior == MappingBehavior.Direct
                          || result.MappingBehavior == MappingBehavior.EndpointIndependent
                          || result.MappingBehavior == MappingBehavior.AddressDependent
                          || result.MappingBehavior == MappingBehavior.AddressAndPortDependent
            );
            Assert.IsTrue(result.FilteringBehavior == FilteringBehavior.EndpointIndependent
                          || result.FilteringBehavior == FilteringBehavior.AddressDependent
                          || result.FilteringBehavior == FilteringBehavior.AddressAndPortDependent
            );
        }
    }
}
