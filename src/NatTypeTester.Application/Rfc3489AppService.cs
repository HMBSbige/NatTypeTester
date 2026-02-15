namespace NatTypeTester.Application;

[UsedImplicitly]
public class Rfc3489AppService : ApplicationService, IRfc3489AppService
{
	private IDnsClient DnsClient => LazyServiceProvider.GetRequiredService<IDnsClient>();

	private DefaultAClient ADnsClient => LazyServiceProvider.GetRequiredService<DefaultAClient>();

	private DefaultAAAAClient AAAADnsClient => LazyServiceProvider.GetRequiredService<DefaultAAAAClient>();

	private StunClient3489? _client;

	public ClassicStunResult? State => _client is { } client ? client.State with { } : null;

	public async Task<ClassicStunResult> TestAsync(StunTestInput input, CancellationToken cancellationToken = default)
	{
		StunServer.TryParse(input.StunServer, out StunServer? server);
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

		using IUdpProxy proxy = ProxyFactory.CreateProxy(input.ProxyType, localEndPoint, socks5Option);
		using StunClient3489 client = new(new IPEndPoint(serverIp, server.Port), localEndPoint, proxy);

		_client = client;

		try
		{
			await client.ConnectProxyAsync(cancellationToken);

			try
			{
				await client.QueryAsync(cancellationToken);
			}
			finally { await client.CloseProxyAsync(cancellationToken); }

			return client.State with { };
		}
		finally
		{
			_client = null;
		}
	}
}
