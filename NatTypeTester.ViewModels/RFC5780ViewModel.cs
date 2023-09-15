using Dns.Net.Abstractions;
using Dns.Net.Clients;
using JetBrains.Annotations;
using Microsoft;
using NatTypeTester.Models;
using ReactiveUI;
using Socks5.Models;
using STUN;
using STUN.Client;
using STUN.Enums;
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

	private StunResult5389 _result5389;
	public StunResult5389 Result5389
	{
		get => _result5389;
		set => this.RaiseAndSetIfChanged(ref _result5389, value);
	}

	private StunResult5389 _udpResult;
	private StunResult5389 _tcpResult;
	private StunResult5389 _tlsResult;

	private TransportType _transportType;
	public TransportType TransportType
	{
		get => _transportType;
		set => this.RaiseAndSetIfChanged(ref _transportType, value);
	}

	public ReactiveCommand<Unit, Unit> DiscoveryNatType { get; }

	public RFC5780ViewModel()
	{
		_udpResult = new StunResult5389();
		_tcpResult = new StunResult5389();
		_tlsResult = new StunResult5389();
		_result5389 = _udpResult;
		DiscoveryNatType = ReactiveCommand.CreateFromTask(DiscoveryNatTypeAsync);
	}

	private async Task DiscoveryNatTypeAsync(CancellationToken token)
	{
		Verify.Operation(StunServer.TryParse(Config.StunServer, out StunServer? server, TransportType is TransportType.Tls ? StunServer.DefaultTlsPort : StunServer.DefaultPort), @"Wrong STUN Server!");

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

		TransportType transport = TransportType;
		if (transport is TransportType.Udp)
		{
			using IUdpProxy proxy = ProxyFactory.CreateProxy(Config.ProxyType, Result5389.LocalEndPoint, socks5Option);
			using StunClient5389UDP client = new(new IPEndPoint(serverIp, server.Port), Result5389.LocalEndPoint, proxy);

			try
			{
				using (Observable.Interval(TimeSpan.FromSeconds(0.1))
						.ObserveOn(RxApp.MainThreadScheduler)
						// ReSharper disable once AccessToDisposedClosure
						.Subscribe(_ => Result5389 = _udpResult = client.State with { }))
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
				Result5389 = _udpResult = client.State with { };
			}
		}
		else
		{
			using ITcpProxy proxy = ProxyFactory.CreateProxy(transport, Config.ProxyType, socks5Option, server.Hostname);
			using IStunClient5389 client = new StunClient5389TCP(new IPEndPoint(serverIp, server.Port), Result5389.LocalEndPoint, proxy);

			try
			{
				using (Observable.Interval(TimeSpan.FromSeconds(0.1))
						.ObserveOn(RxApp.MainThreadScheduler)
						.Subscribe(_ => UpdateData()))
				{
					await client.QueryAsync(token);
				}
			}
			finally
			{
				UpdateData();
			}

			void UpdateData()
			{
				// ReSharper disable once AccessToDisposedClosure
				Result5389 = client.State with { };
				if (transport is TransportType.Tcp)
				{
					_tcpResult = Result5389;
				}
				else
				{
					_tlsResult = Result5389;
				}
			}
		}
	}

	public void ResetResult()
	{
		Result5389 = TransportType switch
		{
			TransportType.Tcp => _tcpResult,
			TransportType.Tls => _tlsResult,
			_ => _udpResult
		};
	}
}
