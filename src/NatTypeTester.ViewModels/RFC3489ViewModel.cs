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
public class RFC3489ViewModel : ViewModelBase, IRoutableViewModel
{
	public string UrlPathSegment => @"RFC3489";
	public IScreen HostScreen => LazyServiceProvider.LazyGetRequiredService<IScreen>();

	private Config Config => LazyServiceProvider.LazyGetRequiredService<Config>();

	private IDnsClient DnsClient => LazyServiceProvider.LazyGetRequiredService<IDnsClient>();
	private IDnsClient AAAADnsClient => LazyServiceProvider.LazyGetRequiredService<DefaultAAAAClient>();
	private IDnsClient ADnsClient => LazyServiceProvider.LazyGetRequiredService<DefaultAClient>();

	private ClassicStunResult _result3489;
	public ClassicStunResult Result3489
	{
		get => _result3489;
		set => this.RaiseAndSetIfChanged(ref _result3489, value);
	}

	public ReactiveCommand<Unit, Unit> TestClassicNatType { get; }

	public RFC3489ViewModel()
	{
		_result3489 = new ClassicStunResult();
		TestClassicNatType = ReactiveCommand.CreateFromTask(TestClassicNatTypeAsync);
	}

	private async Task TestClassicNatTypeAsync(CancellationToken token)
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
		if (Result3489.LocalEndPoint is null)
		{
			serverIp = await DnsClient.QueryAsync(server.Hostname, token);
			Result3489.LocalEndPoint = serverIp.AddressFamily is AddressFamily.InterNetworkV6 ? new IPEndPoint(IPAddress.IPv6Any, IPEndPoint.MinPort) : new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
		}
		else
		{
			if (Result3489.LocalEndPoint.AddressFamily is AddressFamily.InterNetworkV6)
			{
				serverIp = await AAAADnsClient.QueryAsync(server.Hostname, token);
			}
			else
			{
				serverIp = await ADnsClient.QueryAsync(server.Hostname, token);
			}
		}

		using IUdpProxy proxy = ProxyFactory.CreateProxy(Config.ProxyType, Result3489.LocalEndPoint, socks5Option);

		using StunClient3489 client = new(new IPEndPoint(serverIp, server.Port), Result3489.LocalEndPoint, proxy);

		try
		{
			using (Observable.Interval(TimeSpan.FromSeconds(0.1))
					.ObserveOn(RxApp.MainThreadScheduler)
					// ReSharper disable once AccessToDisposedClosure
					.Subscribe(_ => Result3489 = client.State with { }))
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
		}
		finally
		{
			Result3489 = client.State with { };
		}
	}
}
