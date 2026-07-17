namespace NatTypeTester.Application;

internal class StunServerListAppService(IHttpClientFactory httpClientFactory) : IStunServerListAppService
{
	public async Task<List<string>> LoadAsync(LoadStunServerListInput input, CancellationToken cancellationToken = default)
	{
		string[] lines;

		if (Uri.TryCreate(input.Uri, UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			using HttpClient httpClient = AppHttpClientFactory.Create(httpClientFactory, input.Proxy);
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
