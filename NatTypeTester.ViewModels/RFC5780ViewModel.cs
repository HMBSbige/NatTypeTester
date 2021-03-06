using JetBrains.Annotations;
using NatTypeTester.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using STUN.Client;
using STUN.Proxy;
using STUN.StunResult;
using STUN.Utils;
using System;
using System.Net;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace NatTypeTester.ViewModels
{
	[UsedImplicitly]
	public class RFC5780ViewModel : ViewModelBase, IRoutableViewModel
	{
		public string UrlPathSegment => @"RFC5780";
		public IScreen HostScreen => LazyServiceProvider.LazyGetRequiredService<IScreen>();

		private Config Config => LazyServiceProvider.LazyGetRequiredService<Config>();

		[Reactive]
		public StunResult5389 Result5389 { get; set; }

		public ReactiveCommand<Unit, Unit> DiscoveryNatType { get; }

		public RFC5780ViewModel()
		{
			Result5389 = new StunResult5389 { LocalEndPoint = new IPEndPoint(IPAddress.Any, 0) };
			DiscoveryNatType = ReactiveCommand.CreateFromTask(DiscoveryNatTypeImpl);
		}

		private async Task DiscoveryNatTypeImpl(CancellationToken token)
		{
			var server = new StunServer();
			if (!server.Parse(Config.StunServer))
			{
				throw new Exception(@"Wrong STUN Server!");
			}

			using var proxy = ProxyFactory.CreateProxy(
					Config.ProxyType,
					Result5389.LocalEndPoint,
					NetUtils.ParseEndpoint(Config.ProxyServer),
					Config.ProxyUser, Config.ProxyPassword
			);

			using var client = new StunClient5389UDP(server.Hostname, server.Port, Result5389.LocalEndPoint, proxy);

			Result5389 = client.Status;
			await client.QueryAsync();

			var cache = new StunResult5389();
			cache.Clone(client.Status);
			cache.LocalEndPoint = client.LocalEndPoint;
			Result5389 = cache;
		}
	}
}
