using Dns.Net.Abstractions;
using JetBrains.Annotations;
using NatTypeTester.Models;
using ReactiveUI;
using STUN.Client;
using STUN.Proxy;
using STUN.StunResult;
using STUN.Utils;
using System;
using System.Net;
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

		public RFC3489ViewModel()
		{
			Result3489 = new ClassicStunResult { LocalEndPoint = new IPEndPoint(IPAddress.Any, 0) };
			TestClassicNatType = ReactiveCommand.CreateFromTask(TestClassicNatTypeImpl);
		}

		private async Task TestClassicNatTypeImpl(CancellationToken token)
		{
			var server = new StunServer();
			if (!server.Parse(Config.StunServer))
			{
				throw new Exception(@"Wrong STUN Server!");
			}

			using var proxy = ProxyFactory.CreateProxy(
					Config.ProxyType,
					Result3489.LocalEndPoint,
					IPEndPoint.Parse(Config.ProxyServer),
					Config.ProxyUser, Config.ProxyPassword
			);

			using var client = new StunClient3489(DnsClient, server.Hostname, server.Port, Result3489.LocalEndPoint, proxy);

			Result3489 = client.Status;
			using (Observable.Interval(TimeSpan.FromSeconds(0.1))
					.ObserveOn(RxApp.MainThreadScheduler)
					.Subscribe(_ => this.RaisePropertyChanged(nameof(Result3489))))
			{
				await client.Query3489Async();
			}

			Result3489.LocalEndPoint = client.LocalEndPoint;

			this.RaisePropertyChanged(nameof(Result3489));
		}
	}
}
