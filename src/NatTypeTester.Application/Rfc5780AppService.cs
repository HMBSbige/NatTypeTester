namespace NatTypeTester.Application;

[UsedImplicitly]
public class Rfc5780AppService : ApplicationService, IRfc5780AppService
{
	private IDnsClient DnsClient => LazyServiceProvider.GetRequiredService<IDnsClient>();
	private DefaultAClient ADnsClient => LazyServiceProvider.GetRequiredService<DefaultAClient>();
	private DefaultAAAAClient AAAADnsClient => LazyServiceProvider.GetRequiredService<DefaultAAAAClient>();

	private Func<StunResult5389>? _getState;

	public StunResult5389? State => _getState?.Invoke();

	public async Task<StunResult5389> TestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default)
	{
		StunServer.TryParse(input.StunServer, out StunServer? server, transportType is TransportType.Tls ? StunServer.DefaultTlsPort : StunServer.DefaultPort);
		HostnameEndpoint.TryParse(input.ProxyServer, out HostnameEndpoint? proxyIpe);

		Socks5CreateOption socks5Option = new()
		{
			Address = await DnsClient.QueryAsync(proxyIpe!.Hostname, cancellationToken),
			Port = proxyIpe.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = input.ProxyUser,
				Password = input.ProxyPassword
			}
		};

		IPAddress? serverIp;
		IPEndPoint.TryParse(input.LocalEndPoint ?? string.Empty, out IPEndPoint? localEndPoint);

		if (localEndPoint is null)
		{
			serverIp = await DnsClient.QueryAsync(server!.Hostname, cancellationToken);
			localEndPoint = serverIp.AddressFamily is AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort) : new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
		}
		else
		{
			serverIp = localEndPoint.AddressFamily is AddressFamily.InterNetworkV6
				? await AAAADnsClient.QueryAsync(server!.Hostname, cancellationToken)
				: await ADnsClient.QueryAsync(server!.Hostname, cancellationToken);
		}

		try
		{
			if (transportType is TransportType.Udp)
			{
				using IUdpProxy proxy = ProxyFactory.CreateProxy(input.ProxyType, localEndPoint, socks5Option);
				using StunClient5389UDP client = new(new IPEndPoint(serverIp, server.Port), localEndPoint, proxy);

				_getState = () => client.State with { };

				await client.ConnectProxyAsync(cancellationToken);

				try
				{ await client.QueryAsync(cancellationToken); }
				finally { await client.CloseProxyAsync(cancellationToken); }

				return client.State with { };
			}
			else
			{
				using ITcpProxy proxy = ProxyFactory.CreateProxy(transportType, input.ProxyType, socks5Option, server.Hostname);
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(serverIp, server.Port), localEndPoint, proxy);

				_getState = () => client.State with { };

				await client.QueryAsync(cancellationToken);

				return client.State with { };
			}
		}
		finally
		{
			_getState = null;
		}
	}
}
