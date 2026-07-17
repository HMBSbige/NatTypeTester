namespace NatTypeTester.Application;

internal static class AppHttpClientFactory
{
	public static HttpClient Create(IHttpClientFactory httpClientFactory, ProxyOptions proxyOptions)
	{
		if (proxyOptions.Type is ProxyType.Socks5
			&& !string.IsNullOrWhiteSpace(proxyOptions.Server)
			&& HostnameEndpoint.TryParse(proxyOptions.Server, out HostnameEndpoint? proxyEndpoint, NatTypeTesterConsts.DefaultSocks5Port))
		{
			SocketsHttpHandler handler = new();
			WebProxy proxy = new($"socks5://{proxyEndpoint.Hostname}:{proxyEndpoint.Port}");

			if (!string.IsNullOrWhiteSpace(proxyOptions.UserName))
			{
				proxy.Credentials = new NetworkCredential(proxyOptions.UserName, proxyOptions.Password);
			}

			handler.Proxy = proxy;
			return new HttpClient(handler, true);
		}

		return httpClientFactory.CreateClient();
	}
}
