namespace NatTypeTester.Application;

[UsedImplicitly]
public class Rfc5780AppService(
	IDnsClient dnsClient,
	DefaultAClient aDnsClient,
	DefaultAAAAClient aaaaDnsClient) : IRfc5780AppService, ITransientDependency
{
	private Func<StunResult5389>? _getState;

	public StunResult5389? State => _getState?.Invoke();

	public async Task<StunResult5389> TestAsync(
		StunTestInput input,
		StunResult5389 currentResult,
		TransportType transportType,
		CancellationToken cancellationToken = default)
	{
		if (!StunServer.TryParse(input.StunServer, out StunServer? server, transportType is TransportType.Tls ? StunServer.DefaultTlsPort : StunServer.DefaultPort))
		{
			throw new InvalidOperationException("Wrong STUN Server!");
		}

		if (!HostnameEndpoint.TryParse(input.ProxyServer, out HostnameEndpoint? proxyIpe))
		{
			throw new NotSupportedException("Unknown proxy address");
		}

		Socks5CreateOption socks5Option = new()
		{
			Address = await dnsClient.QueryAsync(proxyIpe.Hostname, cancellationToken),
			Port = proxyIpe.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = input.ProxyUser,
				Password = input.ProxyPassword
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

		try
		{
			if (transportType is TransportType.Udp)
			{
				using IUdpProxy proxy = ProxyFactory.CreateProxy(input.ProxyType, currentResult.LocalEndPoint, socks5Option);
				using StunClient5389UDP client = new(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

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
				using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

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
