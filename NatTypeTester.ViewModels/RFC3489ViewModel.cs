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
	public class RFC3489ViewModel : ViewModelBase, IRoutableViewModel
	{
		public string UrlPathSegment => @"RFC3489";
		public IScreen HostScreen => LazyServiceProvider.LazyGetRequiredService<IScreen>();

		private Config Config => LazyServiceProvider.LazyGetRequiredService<Config>();

		[Reactive]
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
					NetUtils.ParseEndpoint(Config.ProxyServer),
					Config.ProxyUser, Config.ProxyPassword
			);

			using var client = new StunClient3489(server.Hostname, server.Port, Result3489.LocalEndPoint, proxy);

			Result3489 = client.Status;
			await client.Query3489Async();

			Result3489.LocalEndPoint = client.LocalEndPoint;
		}
	}
}
