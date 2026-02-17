namespace NatTypeTester.Application;

internal class StunTestInputResolver(IServiceProvider serviceProvider)
{
	private IDnsClient DnsClient => serviceProvider.GetRequiredService<IDnsClient>();

	public static StunServer ParseStunServer(string stunServer, ushort defaultPort = StunServer.DefaultPort)
	{
		if (!StunServer.TryParse(stunServer, out StunServer? server, defaultPort))
		{
			throw new InvalidOperationException($@"Invalid STUN server: {stunServer}");
		}

		return server;
	}

	public async Task<Socks5CreateOption?> ResolveSocks5OptionAsync(StunTestInput input, CancellationToken cancellationToken = default)
	{
		if (input.ProxyType is ProxyType.Plain)
		{
			return null;
		}

		if (!HostnameEndpoint.TryParse(input.ProxyServer ?? string.Empty, out HostnameEndpoint? proxyEndpoint))
		{
			throw new InvalidOperationException($@"Invalid proxy server: {input.ProxyServer}");
		}

		return new Socks5CreateOption
		{
			Address = await DnsClient.QueryAsync(proxyEndpoint.Hostname, cancellationToken),
			Port = proxyEndpoint.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = input.ProxyUser,
				Password = input.ProxyPassword
			}
		};
	}

	public async Task<(IPAddress ServerIp, IPEndPoint LocalEndPoint)> ResolveServerIpAndLocalEndPointAsync(StunServer server, string? localEndPointText, CancellationToken cancellationToken = default)
	{
		IPEndPoint.TryParse(localEndPointText ?? string.Empty, out IPEndPoint? localEndPoint);

		IDnsClient dnsClient = localEndPoint is not null
			? serviceProvider.GetRequiredKeyedService<IDnsClient>(localEndPoint.AddressFamily)
			: DnsClient;

		IPAddress serverIp = await dnsClient.QueryAsync(server.Hostname, cancellationToken);

		localEndPoint ??= new IPEndPoint
		(
			serverIp.AddressFamily is AddressFamily.InterNetworkV6 ? IPAddress.IPv6Any : IPAddress.Any,
			IPEndPoint.MinPort
		);

		return (serverIp, localEndPoint);
	}

	public static async Task QueryWithProxyAsync(IUdpStunClient client, CancellationToken cancellationToken = default)
	{
		await client.ConnectProxyAsync(cancellationToken);

		try
		{
			await client.QueryAsync(cancellationToken);
		}
		finally
		{
			await client.CloseProxyAsync(cancellationToken);
		}
	}
}
