namespace NatTypeTester.Application;

[UsedImplicitly]
public class StunClientAppService(
	IDnsClient dnsClient,
	DefaultAClient aDnsClient,
	DefaultAAAAClient aaaaDnsClient,
	Config config) : ITransientDependency
{
	public async Task<ClassicStunResult> TestClassicNatTypeAsync(
		ClassicStunResult currentResult,
		Action<ClassicStunResult>? onProgress = null,
		CancellationToken cancellationToken = default)
	{
		if (!StunServer.TryParse(config.StunServer, out StunServer? server))
		{
			throw new InvalidOperationException("Wrong STUN Server!");
		}

		if (!HostnameEndpoint.TryParse(config.ProxyServer, out HostnameEndpoint? proxyIpe))
		{
			throw new NotSupportedException("Unknown proxy address");
		}

		Socks5CreateOption socks5Option = new()
		{
			Address = await dnsClient.QueryAsync(proxyIpe.Hostname, cancellationToken),
			Port = proxyIpe.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = config.ProxyUser,
				Password = config.ProxyPassword
			}
		};

		IPAddress? serverIp;

		if (currentResult.LocalEndPoint is null)
		{
			serverIp = await dnsClient.QueryAsync(server.Hostname, cancellationToken);
			currentResult.LocalEndPoint = serverIp.AddressFamily is AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort) : new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
		}
		else
		{
			serverIp = currentResult.LocalEndPoint.AddressFamily is AddressFamily.InterNetworkV6
				? await aaaaDnsClient.QueryAsync(server.Hostname, cancellationToken)
				: await aDnsClient.QueryAsync(server.Hostname, cancellationToken);
		}

		using IUdpProxy proxy = ProxyFactory.CreateProxy(config.ProxyType, currentResult.LocalEndPoint, socks5Option);
		using StunClient3489 client = new(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

		return await RunWithProgressAsync
		(
			() => client.State with { },
			onProgress,
			async ct =>
			{
				await client.ConnectProxyAsync(ct);

				try { await client.QueryAsync(ct); }
				finally { await client.CloseProxyAsync(ct); }
			},
			cancellationToken
		);
	}

	public async Task<StunResult5389> TestRfc5780NatTypeAsync(
		StunResult5389 currentResult,
		TransportType transportType,
		Action<StunResult5389>? onProgress = null,
		CancellationToken cancellationToken = default)
	{
		if (!StunServer.TryParse(config.StunServer, out StunServer? server, transportType is TransportType.Tls ? StunServer.DefaultTlsPort : StunServer.DefaultPort))
		{
			throw new InvalidOperationException("Wrong STUN Server!");
		}

		if (!HostnameEndpoint.TryParse(config.ProxyServer, out HostnameEndpoint? proxyIpe))
		{
			throw new NotSupportedException("Unknown proxy address");
		}

		Socks5CreateOption socks5Option = new()
		{
			Address = await dnsClient.QueryAsync(proxyIpe.Hostname, cancellationToken),
			Port = proxyIpe.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = config.ProxyUser,
				Password = config.ProxyPassword
			}
		};

		IPAddress? serverIp;

		if (currentResult.LocalEndPoint is null)
		{
			serverIp = await dnsClient.QueryAsync(server.Hostname, cancellationToken);
			currentResult.LocalEndPoint = serverIp.AddressFamily is AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort) : new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
		}
		else
		{
			serverIp = currentResult.LocalEndPoint.AddressFamily is AddressFamily.InterNetworkV6
				? await aaaaDnsClient.QueryAsync(server.Hostname, cancellationToken)
				: await aDnsClient.QueryAsync(server.Hostname, cancellationToken);
		}

		if (transportType is TransportType.Udp)
		{
			using IUdpProxy proxy = ProxyFactory.CreateProxy(config.ProxyType, currentResult.LocalEndPoint, socks5Option);
			using StunClient5389UDP client = new(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

			return await RunWithProgressAsync
			(
				() => client.State with { },
				onProgress,
				async ct =>
				{
					await client.ConnectProxyAsync(ct);

					try { await client.QueryAsync(ct); }
					finally { await client.CloseProxyAsync(ct); }
				},
				cancellationToken
			);
		}
		else
		{
			using ITcpProxy proxy = ProxyFactory.CreateProxy(transportType, config.ProxyType, socks5Option, server.Hostname);
			using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

			return await RunWithProgressAsync
			(
				() => client.State with { },
				onProgress,
				async ct => await client.QueryAsync(ct),
				cancellationToken
			);
		}
	}

	private static async Task<TResult> RunWithProgressAsync<TResult>(
		Func<TResult> getState,
		Action<TResult>? onProgress,
		Func<CancellationToken, ValueTask> action,
		CancellationToken cancellationToken)
	{
		using (Observable.Interval(TimeSpan.FromSeconds(0.1))
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe(_ => onProgress?.Invoke(getState())))
		{
			await action(cancellationToken);
		}

		return getState();
	}
}
