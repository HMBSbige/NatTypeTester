namespace NatTypeTester.Application;

[UsedImplicitly]
public class Rfc5780AppService : ApplicationService, IRfc5780AppService
{
	private StunTestInputResolver Resolver => LazyServiceProvider.GetRequiredService<StunTestInputResolver>();

	private IStunClient5389? _client;

	public StunResult5389? State => _client?.State;

	public Task<StunResult5389> BindingTestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default)
	{
		return RunTestAsync(input, transportType, static (client, ct) => client.BindingTestAsync(ct), cancellationToken);
	}

	public Task<StunResult5389> MappingBehaviorTestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default)
	{
		return RunTestAsync
		(
			input,
			transportType,
			static async (client, ct) =>
			{
				await client.MappingBehaviorTestAsync(ct);
				return client.State;
			},
			cancellationToken
		);
	}

	public Task<StunResult5389> FilteringBehaviorTestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default)
	{
		return RunTestAsync
		(
			input,
			transportType,
			static async (client, ct) =>
			{
				await client.FilteringBehaviorTestAsync(ct);
				return client.State;
			},
			cancellationToken
		);
	}

	public Task<StunResult5389> TestAsync(StunTestInput input, TransportType transportType, CancellationToken cancellationToken = default)
	{
		// DTLS: 当前没有服务端支持测试 filtering behavior，仅测试 mapping behavior
		return RunTestAsync
		(
			input,
			transportType,
			transportType switch
			{
				TransportType.Dtls => static async (client, ct) =>
				{
					await client.MappingBehaviorTestAsync(ct);
					return client.State;
				}
				,
				_ => static async (client, ct) =>
				{
					await client.QueryAsync(ct);
					return client.State;
				}
			},
			cancellationToken
		);
	}

	private async Task<StunResult5389> RunTestAsync(
		StunTestInput input,
		TransportType transportType,
		Func<IStunClient5389, CancellationToken, ValueTask<StunResult5389>> testAction,
		CancellationToken cancellationToken)
	{
		StunServer server = StunTestInputResolver.ParseStunServer
		(
			input.StunServer,
			transportType is TransportType.Tls or TransportType.Dtls ? StunServer.DefaultTlsPort : StunServer.DefaultPort
		);

		Socks5CreateOption? socks5CreateOption = await Resolver.ResolveSocks5OptionAsync(input, cancellationToken);

		(IPAddress serverIp, IPEndPoint localEndPoint) = await Resolver.ResolveServerIpAndLocalEndPointAsync(server, input.LocalEndPoint, cancellationToken);

		try
		{
			switch (transportType)
			{
				case TransportType.Dtls:
				case TransportType.Udp:
				{
					await using IUdpProxy proxy = ProxyFactory.CreateProxy(transportType, input.ProxyType, localEndPoint, socks5CreateOption, server.Hostname, input.SkipCertificateValidation);
					await using StunClient5389UDP client = new(new IPEndPoint(serverIp, server.Port), localEndPoint, proxy);

					_client = client;

					await client.ConnectProxyAsync(cancellationToken);

					try
					{
						return await testAction(client, cancellationToken);
					}
					finally
					{
						await client.CloseProxyAsync(cancellationToken);
					}
				}
				case TransportType.Tcp:
				case TransportType.Tls:
				default:
				{
					using ITcpProxy proxy = ProxyFactory.CreateProxy(transportType, input.ProxyType, socks5CreateOption, server.Hostname, input.SkipCertificateValidation);
					using StunClient5389TCP client = new(new IPEndPoint(serverIp, server.Port), localEndPoint, proxy);

					_client = client;

					return await testAction(client, cancellationToken);
				}
			}
		}
		finally
		{
			_client = null;
		}
	}
}
