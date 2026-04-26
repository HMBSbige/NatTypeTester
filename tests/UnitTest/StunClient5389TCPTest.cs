using Dns.Net.Clients;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using System.Net;

namespace UnitTest;

public class StunClient5389TCPTest
{
	private readonly DefaultAClient _dnsClient = new();

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);
	private static readonly HttpClient HttpClient = new();

	private const string Server = "stun.hot-chilli.net";

	[Test]
	public async Task BindingTestSuccessAsync(CancellationToken cancellationToken)
	{
		Skip.When(TestEnvironment.IsCI, "Skipped on CI");

		IPAddress ip = await _dnsClient.QueryAsync(Server, cancellationToken);
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(response.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(response.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(response.PublicEndPoint).IsNotNull();
		await Assert.That(response.LocalEndPoint).IsNotNull();
		await Assert.That(response.OtherEndPoint).IsNotNull();
	}

	[Test]
	public async Task BindingTestFailAsync(CancellationToken cancellationToken)
	{
		IPAddress ip = IPAddress.Parse(@"1.1.1.1");
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any);

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response.BindingTestResult).IsEqualTo(BindingTestResult.Fail);
		await Assert.That(response.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(response.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(response.PublicEndPoint).IsNull();
		await Assert.That(response.LocalEndPoint).IsNull();
		await Assert.That(response.OtherEndPoint).IsNull();
	}

	[Test]
	public async Task TlsBindingTestSuccessAsync(CancellationToken cancellationToken)
	{
		Skip.When(TestEnvironment.IsCI, "Skipped on CI");

		await Assert.That(StunServer.TryParse(Server, out StunServer? stunServer, StunServer.DefaultTlsPort)).IsTrue();
		await Assert.That(stunServer).IsNotNull();
		IPAddress ip = await _dnsClient.QueryAsync(stunServer.Hostname, cancellationToken);
		ITcpProxy tls = new TlsProxy(stunServer.Hostname);
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultPort), Any, tls);

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response.BindingTestResult).IsEqualTo(BindingTestResult.Success);
		await Assert.That(response.MappingBehavior).IsEqualTo(MappingBehavior.Unknown);
		await Assert.That(response.FilteringBehavior).IsEqualTo(FilteringBehavior.Unknown);
		await Assert.That(response.PublicEndPoint).IsNotNull();
		await Assert.That(response.LocalEndPoint).IsNotNull();
		await Assert.That(response.OtherEndPoint).IsNotNull();
	}

	[Test]
	[Explicit]
	public async Task TestServerAsync(CancellationToken cancellationToken)
	{
		const string url = @"https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts_tcp.txt";
		string listRaw = await HttpClient.GetStringAsync(url, cancellationToken);
		string[] list = listRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (string host in list)
		{
			try
			{
				if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, StunServer.DefaultPort))
				{
					continue;
				}

				IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname, cancellationToken);
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, hostEndpoint.Port), Any);

				await client.QueryAsync(cancellationToken);

				if (client.State.MappingBehavior is MappingBehavior.AddressAndPortDependent or MappingBehavior.AddressDependent or MappingBehavior.EndpointIndependent or MappingBehavior.Direct)
				{
					Console.WriteLine(host);
				}
			}
			catch
			{
				// ignored
			}
		}
	}

	[Test]
	[Explicit]
	public async Task TestTlsServerAsync(CancellationToken cancellationToken)
	{
		const string url = @"https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts_tcp.txt";
		string listRaw = await HttpClient.GetStringAsync(url, cancellationToken);
		string[] list = listRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		foreach (string host in list)
		{
			try
			{
				if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, StunServer.DefaultTlsPort))
				{
					continue;
				}

				IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname, cancellationToken);
				ITcpProxy proxy = new TlsProxy(hostEndpoint.Hostname);
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, StunServer.DefaultTlsPort), Any, proxy);

				await client.QueryAsync(cancellationToken);

				if (client.State.MappingBehavior is MappingBehavior.AddressAndPortDependent or MappingBehavior.AddressDependent or MappingBehavior.EndpointIndependent or MappingBehavior.Direct)
				{
					Console.WriteLine(host);
				}
			}
			catch
			{
				// ignored
			}
		}
	}

	[Test]
	public async Task FilteringBehaviorTestAsync(CancellationToken cancellationToken)
	{
		await Assert.That(async () =>
		{
			using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(IPAddress.Loopback, 3478), Any);
			await client.FilteringBehaviorTestAsync(cancellationToken);
		}).Throws<NotSupportedException>();
	}
}
