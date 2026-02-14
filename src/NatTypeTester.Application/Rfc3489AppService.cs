namespace NatTypeTester.Application;

[UsedImplicitly]
public class Rfc3489AppService(
	IDnsClient dnsClient,
	DefaultAClient aDnsClient,
	DefaultAAAAClient aaaaDnsClient) : IRfc3489AppService, ITransientDependency
{
	private StunClient3489? _client;

	public ClassicStunResult? State => _client is { } client ? client.State with { } : null;

	public async Task<ClassicStunResult> TestAsync(
		StunTestInput input,
		ClassicStunResult currentResult,
		CancellationToken cancellationToken = default)
	{
		if (!StunServer.TryParse(input.StunServer, out StunServer? server))
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

		using IUdpProxy proxy = ProxyFactory.CreateProxy(input.ProxyType, currentResult.LocalEndPoint, socks5Option);
		using StunClient3489 client = new(new IPEndPoint(serverIp, server.Port), currentResult.LocalEndPoint, proxy);

		_client = client;
		try
		{
			await client.ConnectProxyAsync(cancellationToken);

			try
			{ await client.QueryAsync(cancellationToken); }
			finally { await client.CloseProxyAsync(cancellationToken); }

			return client.State with { };
		}
		finally
		{
			_client = null;
		}
	}
}
