namespace NatTypeTester.Application;

[UsedImplicitly]
public class StunServerListAppService : ApplicationService, IStunServerListAppService
{
	private IHttpClientFactory HttpClientFactory => LazyServiceProvider.GetRequiredService<IHttpClientFactory>();

	public async Task<List<string>> LoadAsync(LoadStunServerListInput input, CancellationToken cancellationToken = default)
	{
		string[] lines;

		if (Uri.TryCreate(input.Uri, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			using HttpClient httpClient = CreateHttpClient(input);
			string content = await httpClient.GetStringAsync(uri, cancellationToken);
			lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
		}
		else
		{
			lines = await File.ReadAllLinesAsync(input.Uri, cancellationToken);
		}

		List<string> validServers = [];

		foreach (string line in lines)
		{
			if (StunServer.TryParse(line.Trim(), out StunServer? server))
			{
				validServers.Add(server.ToString());
			}
		}

		return validServers;
	}

	private HttpClient CreateHttpClient(LoadStunServerListInput input)
	{
		if (input.ProxyType is ProxyType.Socks5
			&& !string.IsNullOrWhiteSpace(input.ProxyServer)
			&& HostnameEndpoint.TryParse(input.ProxyServer, out HostnameEndpoint? proxyEndpoint, 1080))
		{
			SocketsHttpHandler handler = new();
			WebProxy proxy = new($"socks5://{proxyEndpoint.Hostname}:{proxyEndpoint.Port}");

			if (!string.IsNullOrWhiteSpace(input.ProxyUser))
			{
				proxy.Credentials = new NetworkCredential(input.ProxyUser, input.ProxyPassword);
			}

			handler.Proxy = proxy;
			return new HttpClient(handler, true);
		}

		return HttpClientFactory.CreateClient();
	}
}
