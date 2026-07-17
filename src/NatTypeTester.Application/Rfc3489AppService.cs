namespace NatTypeTester.Application;

internal class Rfc3489AppService(StunTestInputResolver resolver) : IRfc3489AppService
{
	private StunClient3489? _client;

	public ClassicStunResult? State => _client?.State;

	public async Task<ClassicStunResult> TestAsync(StunTestInput input, CancellationToken cancellationToken = default)
	{
		StunServer server = StunTestInputResolver.ParseStunServer(input.StunServer);

		(Socks5CreateOption? socks5CreateOption, IPAddress serverIp, IPEndPoint localEndPoint) = await resolver.ResolveAsync(input, server, cancellationToken);

		await using IUdpProxy proxy = ProxyFactory.CreateProxy(TransportType.Udp, input.Proxy.Type, localEndPoint, socks5CreateOption, server.Hostname, input.SkipCertificateValidation);
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
