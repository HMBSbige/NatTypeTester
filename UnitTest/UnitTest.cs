using Dns.Net.Abstractions;
using Dns.Net.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using System;
using System.Net;
using System.Threading.Tasks;

namespace UnitTest
{
	[TestClass]
	public class UnitTest
	{
		private readonly IDnsClient dnsClient = new DefaultDnsClient();

		[TestMethod]
		public async Task BindingTest()
		{
			var server = await dnsClient.QueryAsync(@"stun.syncthing.net");
			using var client = new StunClient5389UDP(server, 3478, new IPEndPoint(IPAddress.Any, 0));
			await client.BindingTestAsync();
			var result = client.Status;

			Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
			Assert.IsNotNull(result.LocalEndPoint);
			Assert.IsNotNull(result.PublicEndPoint);
			Assert.IsNotNull(result.OtherEndPoint);
			Assert.AreNotEqual(result.LocalEndPoint!.Address, IPAddress.Any);
			Assert.AreEqual(result.MappingBehavior, MappingBehavior.Unknown);
			Assert.AreEqual(result.FilteringBehavior, FilteringBehavior.Unknown);
		}

		[TestMethod]
		public async Task MappingBehaviorTest()
		{
			var server = await dnsClient.QueryAsync(@"stun.syncthing.net");
			using var client = new StunClient5389UDP(server, 3478, new IPEndPoint(IPAddress.Any, 0));
			await client.MappingBehaviorTestAsync();
			var result = client.Status;

			Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
			Assert.IsNotNull(result.LocalEndPoint);
			Assert.IsNotNull(result.PublicEndPoint);
			Assert.IsNotNull(result.OtherEndPoint);
			Assert.AreNotEqual(result.LocalEndPoint!.Address, IPAddress.Any);
			Assert.IsTrue(result.MappingBehavior is
				MappingBehavior.Direct or
				MappingBehavior.EndpointIndependent or
				MappingBehavior.AddressDependent or
				MappingBehavior.AddressAndPortDependent
			);
			Assert.AreEqual(result.FilteringBehavior, FilteringBehavior.Unknown);
		}

		[TestMethod]
		public async Task FilteringBehaviorTest()
		{
			var server = await dnsClient.QueryAsync(@"stun.syncthing.net");
			using var client = new StunClient5389UDP(server, 3478, new IPEndPoint(IPAddress.Any, 0));
			await client.FilteringBehaviorTestAsync();
			var result = client.Status;

			Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
			Assert.IsNotNull(result.LocalEndPoint);
			Assert.IsNotNull(result.PublicEndPoint);
			Assert.IsNotNull(result.OtherEndPoint);
			Assert.AreNotEqual(result.LocalEndPoint!.Address, IPAddress.Any);
			Assert.AreEqual(result.MappingBehavior, MappingBehavior.Unknown);
			Assert.IsTrue(result.FilteringBehavior is
				FilteringBehavior.EndpointIndependent or
				FilteringBehavior.AddressDependent or
				FilteringBehavior.AddressAndPortDependent
			);
		}

		[TestMethod]
		public async Task CombiningTest()
		{
			var server = await dnsClient.QueryAsync(@"stun.syncthing.net");
			using var client = new StunClient5389UDP(server, 3478, new IPEndPoint(IPAddress.Any, 0));
			await client.QueryAsync();
			var result = client.Status;

			Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
			Assert.IsNotNull(result.LocalEndPoint);
			Assert.IsNotNull(result.PublicEndPoint);
			Assert.IsNotNull(result.OtherEndPoint);
			Assert.AreNotEqual(result.LocalEndPoint!.Address, IPAddress.Any);
			Assert.IsTrue(result.MappingBehavior is
				MappingBehavior.Direct or
				MappingBehavior.EndpointIndependent or
				MappingBehavior.AddressDependent or
				MappingBehavior.AddressAndPortDependent
			);
			Assert.IsTrue(result.FilteringBehavior is
				FilteringBehavior.EndpointIndependent or
				FilteringBehavior.AddressDependent or
				FilteringBehavior.AddressAndPortDependent
			);
		}

		[TestMethod]
		public async Task ProxyTest()
		{
			using var proxy = ProxyFactory.CreateProxy(ProxyType.Socks5, IPEndPoint.Parse(@"0.0.0.0:0"), IPEndPoint.Parse(@"127.0.0.1:10000"));
			var server = await dnsClient.QueryAsync(@"stun.syncthing.net");
			using var client = new StunClient5389UDP(server, 3478, new IPEndPoint(IPAddress.Any, 0));
			await client.QueryAsync();
			var result = client.Status;

			Assert.AreEqual(result.BindingTestResult, BindingTestResult.Success);
			Assert.IsNotNull(result.LocalEndPoint);
			Assert.IsNotNull(result.PublicEndPoint);
			Assert.IsNotNull(result.OtherEndPoint);
			Assert.AreNotEqual(result.LocalEndPoint!.Address, IPAddress.Any);
			Assert.IsTrue(
				result.MappingBehavior is MappingBehavior.Direct
				or MappingBehavior.EndpointIndependent
				or MappingBehavior.AddressDependent
				or MappingBehavior.AddressAndPortDependent);
			Assert.IsTrue(
				result.FilteringBehavior is FilteringBehavior.EndpointIndependent
				or FilteringBehavior.AddressDependent
				or FilteringBehavior.AddressAndPortDependent);

			Console.WriteLine(result.BindingTestResult);
			Console.WriteLine(result.MappingBehavior);
			Console.WriteLine(result.FilteringBehavior);
			Console.WriteLine(result.OtherEndPoint);
			Console.WriteLine(result.LocalEndPoint);
			Console.WriteLine(result.PublicEndPoint);
		}
	}
}
