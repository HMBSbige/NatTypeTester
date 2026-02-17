namespace NatTypeTester.Application;

[UsedImplicitly]
public class Rfc5780AppService : ApplicationService, IRfc5780AppService
{
	private StunTestInputResolver Resolver => LazyServiceProvider.GetRequiredService<StunTestInputResolver>();

	private IStunClient5389? _client;

	public StunResult5389? State => _client?.State;

	public async Task<StunResult5389> TestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default)
	{
		StunServer server = StunTestInputResolver.ParseStunServer
		(
			input.StunServer,
			transportType is TransportType.Tls ? StunServer.DefaultTlsPort : StunServer.DefaultPort
		);

		Socks5CreateOption? socks5CreateOption = await Resolver.ResolveSocks5OptionAsync(input, cancellationToken);

		(IPAddress serverIp, IPEndPoint localEndPoint) = await Resolver.ResolveServerIpAndLocalEndPointAsync(server, input.LocalEndPoint, cancellationToken);

		try
		{
			if (transportType is TransportType.Udp)
			{
				using IUdpProxy proxy = ProxyFactory.CreateProxy(input.ProxyType, localEndPoint, socks5CreateOption!);
				using StunClient5389UDP client = new(new IPEndPoint(serverIp, server.Port), localEndPoint, proxy);

				_client = client;

				await StunTestInputResolver.QueryWithProxyAsync(client, cancellationToken);

				return client.State;
			}
			else
			{
				using ITcpProxy proxy = ProxyFactory.CreateProxy(transportType, input.ProxyType, socks5CreateOption!, server.Hostname);
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(serverIp, server.Port), localEndPoint, proxy);

				_client = client;

				await client.QueryAsync(cancellationToken);

				return client.State;
			}
		}
		finally
		{
			_client = null;
		}
	}
}
