using Dns.Net.Abstractions;
using JetBrains.Annotations;
using Microsoft;
using NatTypeTester.Models;
using ReactiveUI;
using Socks5.Models;
using STUN;
using STUN.Client;
using STUN.Proxy;
using STUN.StunResult;
using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NatTypeTester.ViewModels
{
	[UsedImplicitly]
	public class RFC3489ViewModel : ViewModelBase, IRoutableViewModel
	{
		public string UrlPathSegment => @"RFC3489";
		public IScreen HostScreen => LazyServiceProvider.LazyGetRequiredService<IScreen>();

		private Config Config => LazyServiceProvider.LazyGetRequiredService<Config>();

		private IDnsClient DnsClient => LazyServiceProvider.LazyGetRequiredService<IDnsClient>();

		public ClassicStunResult Result3489 { get; set; }

		public ReactiveCommand<Unit, Unit> TestClassicNatType { get; }

		private static readonly IPEndPoint DefaultLocalEndpoint = new(IPAddress.Any, 0);

		public RFC3489ViewModel()
		{
			Result3489 = new ClassicStunResult
			{
				LocalEndPoint = DefaultLocalEndpoint
			};
			TestClassicNatType = ReactiveCommand.CreateFromTask(TestClassicNatTypeAsync);
		}

		private async Task TestClassicNatTypeAsync(CancellationToken token)
		{
			Verify.Operation(StunServer.TryParse(Config.StunServer, out var server), @"Wrong STUN Server!");

			if (!HostnameEndpoint.TryParse(Config.ProxyServer, out var proxyIpe))
			{
				throw new NotSupportedException(@"Unknown proxy address");
			}

			var socks5Option = new Socks5CreateOption
			{
				Address = await DnsClient.QueryAsync(proxyIpe.Hostname, token),
				Port = proxyIpe.Port,
				UsernamePassword = new UsernamePassword
				{
					UserName = Config.ProxyUser,
					Password = Config.ProxyPassword
				}
			};

			Result3489.LocalEndPoint ??= DefaultLocalEndpoint;
			using var proxy = ProxyFactory.CreateProxy(Config.ProxyType, Result3489.LocalEndPoint, socks5Option);

			var ip = await DnsClient.QueryAsync(server.Hostname, token);
			using var client = new StunClient3489(new IPEndPoint(ip, server.Port), Result3489.LocalEndPoint, proxy);

			Result3489 = client.State;
			using (Observable.Interval(TimeSpan.FromSeconds(0.1))
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe(_ => this.RaisePropertyChanged(nameof(Result3489))))
			{
				await client.ConnectProxyAsync(token);
				try
				{
					await client.QueryAsync(token);
					Result3489.LocalEndPoint = new IPEndPoint(client.LocalEndPoint.AddressFamily is AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any, client.LocalEndPoint.Port);
				}
				finally
				{
					await client.CloseProxyAsync(token);
				}
			}

			Result3489 = new ClassicStunResult();
			Result3489.Clone(client.State);

			this.RaisePropertyChanged(nameof(Result3489));
		}
	}
}
