namespace NatTypeTester.Application;

internal static class AppHttpClientFactory
{
	public static HttpClient Create(IHttpClientFactory httpClientFactory, ProxyOptions proxyOptions)
	{
		HostnameEndpoint? proxyEndpoint = proxyOptions.GetSocks5Endpoint();

		if (proxyEndpoint is null)
		{
			return httpClientFactory.CreateClient();
		}

		SocketsHttpHandler handler = new();
		WebProxy proxy = new($"socks5://{proxyEndpoint}");

		if (!string.IsNullOrWhiteSpace(proxyOptions.UserName))
		{
			proxy.Credentials = new NetworkCredential(proxyOptions.UserName, proxyOptions.Password);
		}

		handler.Proxy = proxy;
		return new HttpClient(handler, true);
	}
}
