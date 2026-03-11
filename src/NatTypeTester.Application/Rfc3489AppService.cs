namespace NatTypeTester.Application;

[UsedImplicitly]
public class Rfc3489AppService : ApplicationService, IRfc3489AppService
{
	private StunTestInputResolver Resolver => LazyServiceProvider.GetRequiredService<StunTestInputResolver>();

	private StunClient3489? _client;

	public ClassicStunResult? State => _client?.State;

	public async Task<ClassicStunResult> TestAsync(StunTestInput input, CancellationToken cancellationToken = default)
	{
		StunServer server = StunTestInputResolver.ParseStunServer(input.StunServer);
		Socks5CreateOption? socks5CreateOption = await Resolver.ResolveSocks5OptionAsync(input, cancellationToken);

		(IPAddress serverIp, IPEndPoint localEndPoint) = await Resolver.ResolveServerIpAndLocalEndPointAsync(server, input.LocalEndPoint, cancellationToken);

		await using IUdpProxy proxy = ProxyFactory.CreateProxy(TransportType.Udp, input.ProxyType, localEndPoint, socks5CreateOption, server.Hostname, input.SkipCertificateValidation);
		await using StunClient3489 client = new(new IPEndPoint(serverIp, server.Port), localEndPoint, proxy);

		_client = client;

		try
		{
			await StunTestInputResolver.QueryWithProxyAsync(client, cancellationToken);

			return client.State;
		}
		finally
		{
			_client = null;
		}
	}
}
