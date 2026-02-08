namespace NatTypeTester.Application;

[UsedImplicitly]
public class StunClientAppService : ITransientDependency
{
	private readonly IDnsClient _dnsClient;
	private readonly DefaultAClient _aDnsClient;
	private readonly DefaultAAAAClient _aaaaDnsClient;
	private readonly Config _config;

	public StunClientAppService(
		IDnsClient dnsClient,
		DefaultAClient aDnsClient,
		DefaultAAAAClient aaaaDnsClient,
		Config config)
	{
		_dnsClient = dnsClient;
		_aDnsClient = aDnsClient;
		_aaaaDnsClient = aaaaDnsClient;
		_config = config;
	}

	public async Task<ClassicStunResult> TestClassicNatTypeAsync(
		ClassicStunResult currentResult,
		Action<ClassicStunResult>? onProgress = null,
		CancellationToken cancellationToken = default)
	{
		if (!StunServer.TryParse(_config.StunServer, out StunServer? server))
		{
			throw new InvalidOperationException("Wrong STUN Server!");
		}

		if (!HostnameEndpoint.TryParse(_config.ProxyServer, out HostnameEndpoint? proxyIpe))
		{
			throw new NotSupportedException("Unknown proxy address");
		}

		Socks5CreateOption socks5Option = new()
		{
			Address = await _dnsClient.QueryAsync(proxyIpe.Hostname, cancellationToken),
			Port = proxyIpe.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = _config.ProxyUser,
				Password = _config.ProxyPassword
			}
		};

		IPAddress? serverIp;

		if (currentResult.LocalEndPoint is null)
		{
			serverIp = await _dnsClient.QueryAsync(server.Hostname, cancellationToken);
			currentResult.LocalEndPoint = serverIp.AddressFamily is AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort) : new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
		}
		else
		{
			if (currentResult.LocalEndPoint.AddressFamily is AddressFamily.InterNetworkV6)
			{
				serverIp = await _aaaaDnsClient.QueryAsync(server.Hostname, cancellationToken);
			}
			else
			{
				serverIp = await _aDnsClient.QueryAsync(server.Hostname, cancellationToken);
			}
		}

		using IUdpProxy proxy = ProxyFactory.CreateProxy(_config.ProxyType, currentResult.LocalEndPoint, socks5Option);
		using StunClient3489 client = new(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

		ClassicStunResult result;
		try
		{
			using (Observable.Interval(TimeSpan.FromSeconds(0.1))
						.ObserveOn(RxApp.MainThreadScheduler)
						.Subscribe(_ => onProgress?.Invoke(client.State with { })))
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

			result = client.State with { };
		}
		finally
		{
			result = client.State with { };
		}

		return result;
	}

	public async Task<StunResult5389> TestRfc5780NatTypeAsync(
		StunResult5389 currentResult,
		TransportType transportType,
		Action<StunResult5389>? onProgress = null,
		CancellationToken cancellationToken = default)
	{
		if (!StunServer.TryParse(_config.StunServer, out StunServer? server, transportType is TransportType.Tls ? StunServer.DefaultTlsPort : StunServer.DefaultPort))
		{
			throw new InvalidOperationException("Wrong STUN Server!");
		}

		if (!HostnameEndpoint.TryParse(_config.ProxyServer, out HostnameEndpoint? proxyIpe))
		{
			throw new NotSupportedException("Unknown proxy address");
		}

		Socks5CreateOption socks5Option = new()
		{
			Address = await _dnsClient.QueryAsync(proxyIpe.Hostname, cancellationToken),
			Port = proxyIpe.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = _config.ProxyUser,
				Password = _config.ProxyPassword
			}
		};

		IPAddress? serverIp;

		if (currentResult.LocalEndPoint is null)
		{
			serverIp = await _dnsClient.QueryAsync(server.Hostname, cancellationToken);
			currentResult.LocalEndPoint = serverIp.AddressFamily is AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort) : new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
		}
		else
		{
			if (currentResult.LocalEndPoint.AddressFamily is AddressFamily.InterNetworkV6)
			{
				serverIp = await _aaaaDnsClient.QueryAsync(server.Hostname, cancellationToken);
			}
			else
			{
				serverIp = await _aDnsClient.QueryAsync(server.Hostname, cancellationToken);
			}
		}

		if (transportType is TransportType.Udp)
		{
			using IUdpProxy proxy = ProxyFactory.CreateProxy(_config.ProxyType, currentResult.LocalEndPoint, socks5Option);
			using StunClient5389UDP client = new(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

			StunResult5389 result;
			try
			{
				using (Observable.Interval(TimeSpan.FromSeconds(0.1))
							.ObserveOn(RxApp.MainThreadScheduler)
							.Subscribe(_ => onProgress?.Invoke(client.State with { })))
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

				result = client.State with { };
			}
			finally
			{
				result = client.State with { };
			}

			return result;
		}
		else
		{
			using ITcpProxy proxy = ProxyFactory.CreateProxy(transportType, _config.ProxyType, socks5Option, server.Hostname);
			using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

			StunResult5389 result;
			try
			{
				using (Observable.Interval(TimeSpan.FromSeconds(0.1))
							.ObserveOn(RxApp.MainThreadScheduler)
							.Subscribe(_ => onProgress?.Invoke(client.State with { })))
				{
					await client.QueryAsync(cancellationToken);
				}

				result = client.State with { };
			}
			finally
			{
				result = client.State with { };
			}

			return result;
		}
	}
}
