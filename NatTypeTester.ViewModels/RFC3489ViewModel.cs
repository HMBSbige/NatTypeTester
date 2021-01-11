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
	public class RFC3489ViewModel : ReactiveObject, IRoutableViewModel
	{
		public string UrlPathSegment { get; } = @"RFC3489";
		public IScreen HostScreen { get; }

		private readonly Config _config;

		[Reactive]
		public ClassicStunResult Result3489 { get; set; }

		public ReactiveCommand<Unit, Unit> TestClassicNatType { get; }

		public RFC3489ViewModel(IScreen hostScreen, Config config)
		{
			HostScreen = hostScreen;
			_config = config;

			Result3489 = new ClassicStunResult { LocalEndPoint = new IPEndPoint(IPAddress.Any, 0) };
			TestClassicNatType = ReactiveCommand.CreateFromTask(TestClassicNatTypeImpl);
		}

		private async Task TestClassicNatTypeImpl(CancellationToken token)
		{
			var server = new StunServer();
			if (!server.Parse(_config.StunServer))
			{
				throw new Exception(@"Wrong STUN Server!");
			}

			using var proxy = ProxyFactory.CreateProxy(
					_config.ProxyType,
					Result3489.LocalEndPoint,
					NetUtils.ParseEndpoint(_config.ProxyServer),
					_config.ProxyUser, _config.ProxyPassword
			);

			using var client = new StunClient3489(server.Hostname, server.Port, Result3489.LocalEndPoint, proxy);

			Result3489 = client.Status;
			await client.Query3489Async();

			Result3489.LocalEndPoint = client.LocalEndPoint;
		}
	}
}
