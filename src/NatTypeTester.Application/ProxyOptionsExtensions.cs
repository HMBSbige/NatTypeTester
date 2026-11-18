namespace NatTypeTester.Application;

internal static class ProxyOptionsExtensions
{
	extension(ProxyOptions proxyOptions)
	{
		// STUN 测试和 HTTP 请求共用这套规则：用户选了代理就必须走代理，配置无效时报错，不回退直连
		public HostnameEndpoint? GetSocks5Endpoint()
		{
			return proxyOptions.Type switch
			{
				ProxyType.Plain => null,
				ProxyType.Socks5 when HostnameEndpoint.TryParse(proxyOptions.Server ?? string.Empty, out HostnameEndpoint? endpoint, NatTypeTesterConsts.DefaultSocks5Port) => endpoint,
				ProxyType.Socks5 => throw new ArgumentException(NatTypeTesterLanguage.Current.UnknownProxyAddress),
				_ => throw new NotSupportedException($@"Unsupported proxy type: {proxyOptions.Type}")
			};
		}
	}
}
