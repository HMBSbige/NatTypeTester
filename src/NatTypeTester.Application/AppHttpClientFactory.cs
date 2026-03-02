namespace NatTypeTester.Application;

internal readonly record struct HttpProxyOptions(ProxyType ProxyType, string? ProxyServer, string? ProxyUser, string? ProxyPassword);

internal static class AppHttpClientFactory
{
	public static HttpClient Create(IHttpClientFactory httpClientFactory, HttpProxyOptions proxyOptions)
	{
		if (proxyOptions.ProxyType is ProxyType.Socks5
			&& !string.IsNullOrWhiteSpace(proxyOptions.ProxyServer)
			&& HostnameEndpoint.TryParse(proxyOptions.ProxyServer, out HostnameEndpoint? proxyEndpoint, 1080))
		{
			SocketsHttpHandler handler = new();
			WebProxy proxy = new($"socks5://{proxyEndpoint.Hostname}:{proxyEndpoint.Port}");

			if (!string.IsNullOrWhiteSpace(proxyOptions.ProxyUser))
			{
				proxy.Credentials = new NetworkCredential(proxyOptions.ProxyUser, proxyOptions.ProxyPassword);
			}

			handler.Proxy = proxy;
			return new HttpClient(handler, true);
		}

		return httpClientFactory.CreateClient();
	}
}
