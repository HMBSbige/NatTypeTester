using Dns.Net.Abstractions;
using Dns.Net.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.StunResult;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTest
{
	[TestClass]
	public class StunClien5389UDPTest
	{
		private readonly IDnsClient _dnsClient = new DefaultDnsClient();

		private const string Server = @"stun.syncthing.net";
		private const ushort Port = 3478;

		private static readonly IPEndPoint Any = new(IPAddress.Any, 0);
		private static readonly IPEndPoint LocalAddress1 = IPEndPoint.Parse(@"127.0.0.1:114");
		private static readonly IPEndPoint MappedAddress1 = IPEndPoint.Parse(@"1.1.1.1:114");
		private static readonly IPEndPoint MappedAddress2 = IPEndPoint.Parse(@"1.1.1.1:514");
		private static readonly IPEndPoint ServerAddress = IPEndPoint.Parse(@"2.2.2.2:1919");
		private static readonly IPEndPoint ChangedAddress1 = IPEndPoint.Parse(@"3.3.3.3:23333");
		private static readonly IPEndPoint ChangedAddress2 = IPEndPoint.Parse(@"2.2.2.2:810");
		private static readonly IPEndPoint ChangedAddress3 = IPEndPoint.Parse(@"3.3.3.3:1919");

		private static readonly StunMessage5389 DefaultStunMessage = new();

		[TestMethod]
		public async Task BindingTestSuccessAsync()
		{
			var ip = await _dnsClient.QueryAsync(Server);
			using var client = new StunClient5389UDP(new IPEndPoint(ip, Port), Any);

			var response = await client.BindingTestAsync();

			Assert.AreEqual(BindingTestResult.Success, response.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, response.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, response.FilteringBehavior);
			Assert.IsNotNull(response.PublicEndPoint);
			Assert.IsNotNull(response.LocalEndPoint);
			Assert.IsNotNull(response.OtherEndPoint);
		}

		[TestMethod]
		public async Task BindingTestFailAsync()
		{
			var ip = IPAddress.Parse(@"1.1.1.1");
			using var client = new StunClient5389UDP(new IPEndPoint(ip, Port), Any);

			var response = await client.BindingTestAsync();

			Assert.AreEqual(BindingTestResult.Fail, response.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, response.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, response.FilteringBehavior);
			Assert.IsNull(response.PublicEndPoint);
			Assert.IsNull(response.LocalEndPoint);
			Assert.IsNull(response.OtherEndPoint);
		}

		[TestMethod]
		public async Task MappingBehaviorTestFailAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var fail = new StunResult5389 { BindingTestResult = BindingTestResult.Fail };

			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fail);

			await client.MappingBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Fail, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNull(client.State.PublicEndPoint);
			Assert.IsNull(client.State.LocalEndPoint);
			Assert.IsNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task MappingBehaviorTestUnsupportedServerAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			await TestAsync();

			var r2 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress2
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r2);
			await TestAsync();

			var r3 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress3
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r3);
			await TestAsync();

			async Task TestAsync()
			{
				await client.MappingBehaviorTestAsync();

				Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
				Assert.AreEqual(MappingBehavior.UnsupportedServer, client.State.MappingBehavior);
				Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
				Assert.IsNotNull(client.State.PublicEndPoint);
				Assert.IsNotNull(client.State.LocalEndPoint);
			}
		}

		[TestMethod]
		public async Task MappingBehaviorTestDirectAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var response = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = MappedAddress1,
				OtherEndPoint = ChangedAddress1
			};

			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(response);

			await client.MappingBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Direct, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task MappingBehaviorTestEndpointIndependentAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			await client.MappingBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.EndpointIndependent, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task MappingBehaviorTest2FailAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r2 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Fail,
			};

			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ServerAddress)), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress3)), It.IsAny<CancellationToken>())).ReturnsAsync(r2);
			await client.MappingBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Fail, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task MappingBehaviorTestAddressDependentAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r2 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress2,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r3 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress2,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ServerAddress)), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress3)), It.IsAny<CancellationToken>())).ReturnsAsync(r2);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress1)), It.IsAny<CancellationToken>())).ReturnsAsync(r3);

			await client.MappingBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.AddressDependent, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task MappingBehaviorTestAddressAndPortDependentAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r2 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress2,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r3 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ServerAddress)), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress3)), It.IsAny<CancellationToken>())).ReturnsAsync(r2);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress1)), It.IsAny<CancellationToken>())).ReturnsAsync(r3);

			await client.MappingBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.AddressAndPortDependent, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task MappingBehaviorTest3FailAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r2 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress2,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r3 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Fail
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ServerAddress)), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress3)), It.IsAny<CancellationToken>())).ReturnsAsync(r2);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress1)), It.IsAny<CancellationToken>())).ReturnsAsync(r3);

			await client.MappingBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Fail, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task FilteringBehaviorTestFailAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var fail = new StunResult5389 { BindingTestResult = BindingTestResult.Fail };

			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fail);

			await client.FilteringBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Fail, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNull(client.State.PublicEndPoint);
			Assert.IsNull(client.State.LocalEndPoint);
			Assert.IsNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task FilteringBehaviorTestUnsupportedServerAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			await TestAsync();

			var r2 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress2
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r2);
			await TestAsync();

			var r3 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress3
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r3);
			await TestAsync();

			async Task TestAsync()
			{
				await client.FilteringBehaviorTestAsync();

				Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
				Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
				Assert.AreEqual(FilteringBehavior.UnsupportedServer, client.State.FilteringBehavior);
				Assert.IsNotNull(client.State.PublicEndPoint);
				Assert.IsNotNull(client.State.LocalEndPoint);
			}
		}

		[TestMethod]
		public async Task FilteringBehaviorTestEndpointIndependentAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r2 = new StunResponse(DefaultStunMessage, ChangedAddress1, LocalAddress1.Address);
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(r2);

			await client.FilteringBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.EndpointIndependent, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task FilteringBehaviorTest2UnsupportedServerAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r2 = new StunResponse(DefaultStunMessage, ServerAddress, LocalAddress1.Address);
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(r2);

			await client.FilteringBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.UnsupportedServer, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task FilteringBehaviorTestAddressAndPortDependentAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
			mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

			await client.FilteringBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task FilteringBehaviorTestAddressDependentAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r3 = new StunResponse(DefaultStunMessage, ChangedAddress2, LocalAddress1.Address);
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
			mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(r3);

			await client.FilteringBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.AddressDependent, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task FilteringBehaviorTest3UnsupportedServerAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r3 = new StunResponse(DefaultStunMessage, ServerAddress, LocalAddress1.Address);
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
			mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(r3);

			await client.FilteringBehaviorTestAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.UnsupportedServer, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task QueryFailTestAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var fail = new StunResult5389 { BindingTestResult = BindingTestResult.Fail };

			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(fail);

			await client.QueryAsync();

			Assert.AreEqual(BindingTestResult.Fail, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.Unknown, client.State.FilteringBehavior);
			Assert.IsNull(client.State.PublicEndPoint);
			Assert.IsNull(client.State.LocalEndPoint);
			Assert.IsNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task QueryUnsupportedServerTestAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ServerAddress
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);

			await client.QueryAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Unknown, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.UnsupportedServer, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
		}

		[TestMethod]
		public async Task QueryMappingBehaviorDirectTestAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = MappedAddress1,
				OtherEndPoint = ChangedAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ServerAddress)), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
			mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

			await client.QueryAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.Direct, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task QueryMappingBehaviorEndpointIndependentTestAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.IsAny<IPEndPoint>(), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
			mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

			await client.QueryAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.EndpointIndependent, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}

		[TestMethod]
		public async Task QueryMappingBehaviorAddressAndPortDependentTestAsync()
		{
			var mock = new Mock<StunClient5389UDP>(ServerAddress, Any, default);
			var client = mock.Object;

			var r1 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r2 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress2,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			var r3 = new StunResult5389
			{
				BindingTestResult = BindingTestResult.Success,
				PublicEndPoint = MappedAddress1,
				LocalEndPoint = LocalAddress1,
				OtherEndPoint = ChangedAddress1
			};
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ServerAddress)), It.IsAny<CancellationToken>())).ReturnsAsync(r1);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress3)), It.IsAny<CancellationToken>())).ReturnsAsync(r2);
			mock.Setup(x => x.BindingTestBaseAsync(It.Is<IPEndPoint>(p => Equals(p, ChangedAddress1)), It.IsAny<CancellationToken>())).ReturnsAsync(r3);
			mock.Setup(x => x.FilteringBehaviorTest2Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));
			mock.Setup(x => x.FilteringBehaviorTest3Async(It.IsAny<CancellationToken>())).ReturnsAsync(default(StunResponse?));

			await client.QueryAsync();

			Assert.AreEqual(BindingTestResult.Success, client.State.BindingTestResult);
			Assert.AreEqual(MappingBehavior.AddressAndPortDependent, client.State.MappingBehavior);
			Assert.AreEqual(FilteringBehavior.AddressAndPortDependent, client.State.FilteringBehavior);
			Assert.IsNotNull(client.State.PublicEndPoint);
			Assert.IsNotNull(client.State.LocalEndPoint);
			Assert.IsNotNull(client.State.OtherEndPoint);
		}
	}
}
