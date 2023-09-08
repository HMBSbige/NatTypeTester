using Dns.Net.Abstractions;
using Dns.Net.Clients;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.StunResult;
using System.Net;

namespace UnitTest;

[TestClass]
public class StunClient5389TCPTest
{
	private readonly IDnsClient _dnsClient = new DefaultDnsClient();

	private const string Server = @"stunserver.stunprotocol.org";
	private const ushort Port = 3478;

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);

	[TestMethod]
	public async Task BindingTestSuccessAsync()
	{
		IPAddress ip = await _dnsClient.QueryAsync(Server);
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, Port), Any);

		StunResult5389 response = await client.BindingTestAsync();

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
		IPAddress ip = IPAddress.Parse(@"1.1.1.1");
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, Port), Any);

		StunResult5389 response = await client.BindingTestAsync();

		Assert.AreEqual(BindingTestResult.Fail, response.BindingTestResult);
		Assert.AreEqual(MappingBehavior.Unknown, response.MappingBehavior);
		Assert.AreEqual(FilteringBehavior.Unknown, response.FilteringBehavior);
		Assert.IsNull(response.PublicEndPoint);
		Assert.IsNull(response.LocalEndPoint);
		Assert.IsNull(response.OtherEndPoint);
	}

	[TestMethod]
	public async Task TestServerAsync()
	{
		const string url = @"https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts_tcp.txt";
		HttpClient httpClient = new();
		string listRaw = await httpClient.GetStringAsync(url);
		string[] list = listRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (string host in list)
		{
			if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, 3478))
			{
				continue;
			}

			IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname);
			using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, hostEndpoint.Port), Any);
			try
			{
				await client.QueryAsync();
			}
			catch
			{
				// ignored
			}

			if (client.State.MappingBehavior is MappingBehavior.AddressAndPortDependent or MappingBehavior.AddressDependent or MappingBehavior.EndpointIndependent or MappingBehavior.Direct)
			{
				Console.WriteLine(host);
			}
		}
	}
}
