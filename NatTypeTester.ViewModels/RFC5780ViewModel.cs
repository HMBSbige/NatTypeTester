using Dns.Net.Abstractions;
using Dns.Net.Clients;
using JetBrains.Annotations;
using Microsoft;
using NatTypeTester.Models;
using ReactiveUI;
using Socks5.Models;
using STUN;
using STUN.Client;
using STUN.Proxy;
using STUN.StunResult;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;

namespace NatTypeTester.ViewModels;

[UsedImplicitly]
public class RFC5780ViewModel : ViewModelBase, IRoutableViewModel
{
	public string UrlPathSegment => @"RFC5780";
	public IScreen HostScreen => LazyServiceProvider.LazyGetRequiredService<IScreen>();

	private Config Config => LazyServiceProvider.LazyGetRequiredService<Config>();

	private IDnsClient DnsClient => LazyServiceProvider.LazyGetRequiredService<IDnsClient>();
	private IDnsClient AAAADnsClient => LazyServiceProvider.LazyGetRequiredService<DefaultAAAAClient>();
	private IDnsClient ADnsClient => LazyServiceProvider.LazyGetRequiredService<DefaultAClient>();

	public StunResult5389 Result5389 { get; set; }

	public ReactiveCommand<Unit, Unit> DiscoveryNatType { get; }

	public RFC5780ViewModel()
	{
		Result5389 = new StunResult5389();
		DiscoveryNatType = ReactiveCommand.CreateFromTask(DiscoveryNatTypeAsync);
	}

	private async Task DiscoveryNatTypeAsync(CancellationToken token)
	{
		Verify.Operation(StunServer.TryParse(Config.StunServer, out StunServer? server), @"Wrong STUN Server!");

		if (!HostnameEndpoint.TryParse(Config.ProxyServer, out HostnameEndpoint? proxyIpe))
		{
			throw new NotSupportedException(@"Unknown proxy address");
		}

		Socks5CreateOption socks5Option = new()
		{
			Address = await DnsClient.QueryAsync(proxyIpe.Hostname, token),
			Port = proxyIpe.Port,
			UsernamePassword = new UsernamePassword
			{
				UserName = Config.ProxyUser,
				Password = Config.ProxyPassword
			}
		};

		IPAddress? serverIp;
		if (Result5389.LocalEndPoint is null)
		{
			serverIp = await DnsClient.QueryAsync(server.Hostname, token);
			Result5389.LocalEndPoint = serverIp.AddressFamily is AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort) : new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
		}
		else
		{
			if (Result5389.LocalEndPoint.AddressFamily is AddressFamily.InterNetworkV6)
			{
				serverIp = await AAAADnsClient.QueryAsync(server.Hostname, token);
			}
			else
			{
				serverIp = await ADnsClient.QueryAsync(server.Hostname, token);
			}
		}

		using IUdpProxy proxy = ProxyFactory.CreateProxy(Config.ProxyType, Result5389.LocalEndPoint, socks5Option);

		using StunClient5389UDP client = new(new IPEndPoint(serverIp, server.Port), Result5389.LocalEndPoint, proxy);

		Result5389 = client.State;
		using (Observable.Interval(TimeSpan.FromSeconds(0.1))
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(_ => this.RaisePropertyChanged(nameof(Result5389))))
		{
			await client.ConnectProxyAsync(token);
			try
			{
				await client.QueryAsync(token);
			}
			finally
			{
				await client.CloseProxyAsync(token);
			}
		}

		Result5389 = new StunResult5389();
		Result5389.Clone(client.State);

		this.RaisePropertyChanged(nameof(Result5389));
	}
}
