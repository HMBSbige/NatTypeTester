using Dns.Net.Abstractions;
using Dns.Net.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.Utils;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using static STUN.Utils.AttributeExtensions;

namespace UnitTest
{
	[TestClass]
	public class StunClient3489Test
	{
		private readonly IDnsClient _dnsClient = new DefaultDnsClient();

		private const string Server = @"stun.syncthing.net";
		private const ushort Port = 3478;

		private static readonly IPEndPoint Any = new(IPAddress.Any, 0);
		private static readonly IPEndPoint MappedAddress1 = IPEndPoint.Parse(@"1.1.1.1:114");
		private static readonly IPEndPoint MappedAddress2 = IPEndPoint.Parse(@"1.1.1.1:514");
		private static readonly IPEndPoint ServerAddress = IPEndPoint.Parse(@"2.2.2.2:1919");
		private static readonly IPEndPoint ChangedAddress1 = IPEndPoint.Parse(@"3.3.3.3:23333");
		private static readonly IPEndPoint ChangedAddress2 = IPEndPoint.Parse(@"2.2.2.2:810");

		private static readonly StunMessage5389 DefaultStunMessage = new();

		[TestMethod]
		public async Task UdpBlockedTestAsync()
		{
			var nullMessage = new StunResponse { Message = null, Remote = Any };
			var nullRemote = new StunResponse { Message = DefaultStunMessage, Remote = null };

			var mock = new Mock<StunClient3489>(IPAddress.Any, Port, null, null);
			var client = mock.Object;

			mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).Returns(null);
			await TestAsync();

			mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(nullMessage);
			await TestAsync();

			mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(nullRemote);
			await TestAsync();

			async Task TestAsync()
			{
				Assert.AreEqual(NatType.Unknown, client.Status.NatType);
				await client.QueryAsync();
				Assert.AreEqual(NatType.UdpBlocked, client.Status.NatType);
				client.Status.Reset();
			}
		}

		[TestMethod]
		public async Task UnsupportedServerTest1Async()
		{
			var mock = new Mock<StunClient3489>(IPAddress.Any, Port, null, null);
			var client = mock.Object;

			var unknownResponse = new StunResponse { Message = DefaultStunMessage, Remote = Any };
			mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(unknownResponse);
			await TestAsync();

			var r1 = new StunResponse
			{
				Message = new StunMessage5389
				{
					Attributes = new[]
					{
						BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port)
					}
				},
				Remote = Any
			};
			mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			await TestAsync();

			var r2 = new StunResponse
			{
				Message = new StunMessage5389
				{
					Attributes = new[]
					{
						BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ChangedAddress1.Port)
					}
				},
				Remote = Any
			};
			mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r2);
			await TestAsync();

			var r3 = new StunResponse
			{
				Message = new StunMessage5389
				{
					Attributes = new[]
					{
						BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
						BuildChangeAddress(IpFamily.IPv4, ServerAddress.Address, (ushort)ChangedAddress1.Port)
					}
				},
				Remote = ServerAddress
			};
			mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r3);
			await TestAsync();

			var r4 = new StunResponse
			{
				Message = new StunMessage5389
				{
					Attributes = new[]
					{
						BuildMapping(IpFamily.IPv4, MappedAddress1.Address, (ushort)MappedAddress1.Port),
						BuildChangeAddress(IpFamily.IPv4, ChangedAddress1.Address, (ushort)ServerAddress.Port)
					}
				},
				Remote = ServerAddress
			};
			mock.Setup(x => x.Test1Async(It.IsAny<CancellationToken>())).ReturnsAsync(r4);
			await TestAsync();

			async Task TestAsync()
			{
				Assert.AreEqual(NatType.Unknown, client.Status.NatType);
				await client.QueryAsync();
				Assert.AreEqual(NatType.UnsupportedServer, client.Status.NatType);
				client.Status.Reset();
			}
		}

		[TestMethod]
		public async Task Test1Async()
		{
			var ip = await _dnsClient.QueryAsync(Server);
			using var client = new StunClient3489(ip);
			var response = await client.Test1Async(default);

			Assert.IsNotNull(response);
			Assert.IsNotNull(response.Message);
			Assert.IsNotNull(response.Remote);
			Assert.IsNotNull(response.LocalAddress);

			Assert.AreEqual(ip, response.Remote.Address);
			Assert.AreEqual(Port, response.Remote.Port);

			var mappedAddress = response.Message.GetMappedAddressAttribute();
			var changedAddress = response.Message.GetChangedAddressAttribute();

			Assert.IsNotNull(mappedAddress);
			Assert.IsNotNull(changedAddress);

			Assert.AreNotEqual(ip, changedAddress.Address);
			Assert.AreNotEqual(Port, changedAddress.Port);
		}
	}
}
