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
			HttpProxyOptions proxyOptions = new(input.ProxyType, input.ProxyServer, input.ProxyUser, input.ProxyPassword);
			using HttpClient httpClient = AppHttpClientFactory.Create(HttpClientFactory, proxyOptions);
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
}
