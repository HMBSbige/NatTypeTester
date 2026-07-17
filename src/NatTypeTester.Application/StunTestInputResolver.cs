namespace NatTypeTester.Application;

internal class StunTestInputResolver(IServiceProvider serviceProvider)
{
	private IDnsClient DnsClient => serviceProvider.GetRequiredService<IDnsClient>();

	public static StunServer ParseStunServer(string stunServer, ushort defaultPort = StunServer.DefaultPort)
	{
		if (!StunServer.TryParse(stunServer, out StunServer? server, defaultPort))
		{
			throw new ArgumentException(NatTypeTesterLanguage.Current.WrongStunServer);
		}

		return server;
	}

	public async Task<(Socks5CreateOption? Socks5Option, IPAddress ServerIp, IPEndPoint LocalEndPoint)> ResolveAsync(StunTestInput input, StunServer server, CancellationToken cancellationToken = default)
	{
		// 代理与 STUN 服务器的 DNS 解析互不依赖，并行执行
		Task<Socks5CreateOption?> socks5Task = ResolveSocks5OptionAsync(input.Proxy, cancellationToken);
		Task<(IPAddress, IPEndPoint)> endPointTask = ResolveServerIpAndLocalEndPointAsync(server, input.LocalEndPoint, cancellationToken);
		await Task.WhenAll(socks5Task, endPointTask);

		Socks5CreateOption? socks5CreateOption = await socks5Task;
		(IPAddress serverIp, IPEndPoint localEndPoint) = await endPointTask;

		return (socks5CreateOption, serverIp, localEndPoint);
	}

	private async Task<Socks5CreateOption?> ResolveSocks5OptionAsync(ProxyOptions proxyOptions, CancellationToken cancellationToken = default)
	{
		if (proxyOptions.Type is ProxyType.Plain)
		{
			return null;
		}

		if (!HostnameEndpoint.TryParse(proxyOptions.Server ?? string.Empty, out HostnameEndpoint? proxyEndpoint, NatTypeTesterConsts.DefaultSocks5Port))
		{
			throw new ArgumentException(NatTypeTesterLanguage.Current.UnknownProxyAddress);
		}

		return new Socks5CreateOption
		{
			Address = await DnsClient.QueryAsync(proxyEndpoint.Hostname, cancellationToken),
			Port = proxyEndpoint.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = proxyOptions.UserName,
				Password = proxyOptions.Password
			}
		};
	}

	private async Task<(IPAddress ServerIp, IPEndPoint LocalEndPoint)> ResolveServerIpAndLocalEndPointAsync(StunServer server, string? localEndPointText, CancellationToken cancellationToken = default)
	{
		IPEndPoint? localEndPoint = null;
		if (!string.IsNullOrWhiteSpace(localEndPointText) && !IPEndPoint.TryParse(localEndPointText, out localEndPoint))
		{
			throw new ArgumentException(NatTypeTesterLanguage.Current.InvalidLocalEndPoint);
		}

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
