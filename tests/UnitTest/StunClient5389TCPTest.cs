using Dns.Net.Clients;
using Socks5.Enums;
using Socks5.Models;
using STUN;
using STUN.Client;
using STUN.Enums;
using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace UnitTest;

public class StunClient5389TCPTest
{
	private readonly DefaultAClient _dnsClient = new();

	private static readonly IPEndPoint Any = new(IPAddress.Any, 0);

	private static readonly IPEndPoint Loopback = new(IPAddress.Loopback, 0);

	private static readonly IPEndPoint Unreached = new(IPAddress.Loopback, StunServer.DefaultPort);

	private static readonly TimeSpan ShortReceiveTimeout = TimeSpan.FromMilliseconds(500);

	[Test]
	[MatrixDataSource]
	public async Task BindingTestSuccessAsync([Matrix] bool useTls, CancellationToken cancellationToken)
	{
		Skip.When(TestEnvironment.IsCI, "Skipped on CI");

		IPAddress ip = await _dnsClient.QueryAsync(TestEnvironment.StunServerHost, cancellationToken);
		ushort port = useTls ? StunServer.DefaultTlsPort : StunServer.DefaultPort;
		ITcpProxy? proxy = useTls ? new TlsProxy(TestEnvironment.StunServerHost) : null;
		using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, port), Any, proxy);

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

		await Assert.That(response).IsEqualTo(new StunResult5389
		{
			BindingTestResult = BindingTestResult.Fail
		});
	}

	[Test]
	[Timeout(10_000)]
	[MatrixDataSource]
	public async Task BindingTestReceiveTimeoutAsync([Matrix] bool sendGarbage, CancellationToken cancellationToken)
	{
		await using UnresponsiveTcpServer server = new(sendGarbage);
		using IStunClient5389 client = new StunClient5389TCP(server.EndPoint, Loopback) { ReceiveTimeout = ShortReceiveTimeout };

		long start = Stopwatch.GetTimestamp();
		StunResult5389 response = await client.BindingTestAsync(cancellationToken);
		TimeSpan elapsed = Stopwatch.GetElapsedTime(start);

		await Assert.That(response).IsEqualTo(new StunResult5389
		{
			BindingTestResult = BindingTestResult.Fail
		});
		// The server keeps the connection open, so only the receive timeout can end the request,
		// and not before it elapses (with slack for coarse timers)
		await Assert.That(elapsed).IsGreaterThanOrEqualTo(ShortReceiveTimeout / 2);
		await Assert.That(() => server.ClientClosed).CompletesWithin(TimeSpan.FromSeconds(5));
	}

	[Test]
	[Timeout(10_000)]
	[MatrixDataSource]
	public async Task BindingTestFailsWhenServerRefusesConnectionAsync([Matrix] bool useTls, CancellationToken cancellationToken)
	{
		ITcpProxy? proxy = useTls ? new TlsProxy(@"localhost") : null;
		// Without ConnectTimeout, only the refusal can end the connection attempt
		using IStunClient5389 client = new StunClient5389TCP(GetClosedEndPoint(), Loopback, proxy) { ConnectTimeout = Timeout.InfiniteTimeSpan };

		StunResult5389 response = await client.BindingTestAsync(cancellationToken);

		await Assert.That(response).IsEqualTo(new StunResult5389
		{
			BindingTestResult = BindingTestResult.Fail
		});
	}

	[Test]
	[Timeout(10_000)]
	public async Task BindingTestFailsWhenSocks5ProxyReportsServerRefusedAsync(CancellationToken cancellationToken)
	{
		using TcpListener proxyListener = new(IPAddress.Loopback, 0);
		proxyListener.Start();
		using IStunClient5389 client = new StunClient5389TCP(Unreached, Loopback, CreateSocks5Proxy((IPEndPoint)proxyListener.LocalEndpoint));

		Task<StunResult5389> test = client.BindingTestAsync(cancellationToken).AsTask();
		await ReplyConnectionRefusedAsync(proxyListener, cancellationToken);

		await Assert.That(await test).IsEqualTo(new StunResult5389
		{
			BindingTestResult = BindingTestResult.Fail
		});
	}

	[Test]
	[Timeout(10_000)]
	public async Task BindingTestThrowsWhenSocks5ProxyRefusesConnectionAsync(CancellationToken cancellationToken)
	{
		using IStunClient5389 client = new StunClient5389TCP(Unreached, Loopback, CreateSocks5Proxy(GetClosedEndPoint())) { ConnectTimeout = Timeout.InfiniteTimeSpan };

		// An unreachable proxy is a configuration problem, not a result of the NAT test
		await Assert.That(async () => await client.BindingTestAsync(cancellationToken)).Throws<SocketException>();
	}

	[Test]
	[Explicit]
	[CombinedDataSources]
	public async Task TestServerAsync
	(
		[ClassDataSource<StunServerList>(Shared = SharedType.PerClass)]
		StunServerList serverList,
		[Arguments(false, true)] bool useTls,
		CancellationToken cancellationToken
	)
	{
		ushort defaultPort = useTls ? StunServer.DefaultTlsPort : StunServer.DefaultPort;

		foreach (string host in serverList.Hosts)
		{
			try
			{
				if (!HostnameEndpoint.TryParse(host, out HostnameEndpoint? hostEndpoint, defaultPort))
				{
					continue;
				}

				IPAddress ip = await _dnsClient.QueryAsync(hostEndpoint.Hostname, cancellationToken);
				ushort port = useTls ? StunServer.DefaultTlsPort : hostEndpoint.Port;
				ITcpProxy? proxy = useTls ? new TlsProxy(hostEndpoint.Hostname) : null;
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(ip, port), Any, proxy);

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
		await Assert.That
		(async () =>
			{
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(IPAddress.Loopback, StunServer.DefaultPort), Any);
				await client.FilteringBehaviorTestAsync(cancellationToken);
			}
		).Throws<NotSupportedException>();
	}

	private static IPEndPoint GetClosedEndPoint()
	{
		using TcpListener listener = new(IPAddress.Loopback, 0);
		listener.Start();
		return (IPEndPoint)listener.LocalEndpoint;
	}

	private static Socks5TcpProxy CreateSocks5Proxy(IPEndPoint proxy)
	{
		return new Socks5TcpProxy(new Socks5CreateOption
		{
			Address = proxy.Address,
			Port = (ushort)proxy.Port
		});
	}

	private static async Task ReplyConnectionRefusedAsync(TcpListener listener, CancellationToken cancellationToken)
	{
		using TcpClient connection = await listener.AcceptTcpClientAsync(cancellationToken);
		NetworkStream stream = connection.GetStream();

		// VER | NMETHODS | METHODS, answered with VER | METHOD: no authentication
		byte[] greeting = new byte[2];
		await stream.ReadExactlyAsync(greeting, cancellationToken);
		await stream.ReadExactlyAsync(new byte[greeting[1]], cancellationToken);
		await stream.WriteAsync(new byte[] { 5, 0 }, cancellationToken);

		// VER | CMD | RSV | ATYP | DST.ADDR | DST.PORT, answered with VER | REP | RSV | ATYP | BND.ADDR | BND.PORT
		await stream.ReadExactlyAsync(new byte[4 + 4 + 2], cancellationToken);
		await stream.WriteAsync(new byte[] { 5, (byte)Socks5Reply.ConnectionRefused, 0, 1, 0, 0, 0, 0, 0, 0 }, cancellationToken);
	}

	private sealed class UnresponsiveTcpServer : IAsyncDisposable
	{
		private static readonly TimeSpan GarbageInterval = TimeSpan.FromMilliseconds(100);

		private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
		private readonly CancellationTokenSource _stopCts = new();

		public IPEndPoint EndPoint { get; }

		public Task ClientClosed { get; }

		public UnresponsiveTcpServer(bool sendGarbage)
		{
			_listener.Start();
			EndPoint = (IPEndPoint)_listener.LocalEndpoint;
			ClientClosed = ServeAsync(sendGarbage, _stopCts.Token);
		}

		private async Task ServeAsync(bool sendGarbage, CancellationToken cancellationToken)
		{
			using Socket socket = await _listener.AcceptSocketAsync(cancellationToken);

			try
			{
				if (sendGarbage)
				{
					while (true)
					{
						await socket.SendAsync(RawStunMessage.NotStun, cancellationToken);
						await Task.Delay(GarbageInterval, cancellationToken);
					}
				}
				else
				{
					byte[] buffer = new byte[StunMessage5389.HeaderLength];

					while (await socket.ReceiveAsync(buffer, cancellationToken) > 0)
					{
						// Discard the request and never reply
					}
				}
			}
			catch (SocketException)
			{
				// The client reset the connection
			}
		}

		public async ValueTask DisposeAsync()
		{
			await _stopCts.CancelAsync();

			try
			{
				await ClientClosed;
			}
			catch (OperationCanceledException)
			{
				// Stopped while the connection was still open, or before the client connected
			}
			finally
			{
				_listener.Dispose();
				_stopCts.Dispose();
			}
		}
	}
}
