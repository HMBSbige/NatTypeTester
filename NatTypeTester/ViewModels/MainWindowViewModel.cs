using DynamicData;
using DynamicData.Binding;
using NatTypeTester.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.StunResult;
using STUN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace NatTypeTester.ViewModels
{
	public class MainWindowViewModel : ReactiveObject
	{
		#region RFC3489

		[Reactive]
		public ClassicStunResult Result3489 { get; set; }

		public ReactiveCommand<Unit, Unit> TestClassicNatType { get; }

		#endregion

		#region RFC5780

		[Reactive]
		public StunResult5389 Result5389 { get; set; }

		public ReactiveCommand<Unit, Unit> DiscoveryNatType { get; }

		#endregion

		#region Servers

		[Reactive]
		public string StunServer { get; set; } = @"stun.syncthing.net";

		private readonly IEnumerable<string> _defaultServers = new HashSet<string>
		{
				@"stun.syncthing.net",
				@"stun.qq.com",
				@"stun.miwifi.com",
				@"stun.bige0.com",
				@"stun.stunprotocol.org"
		};

		private SourceList<string> List { get; } = new();
		public readonly IObservableCollection<string> StunServers = new ObservableCollectionExtended<string>();

		#endregion

		#region Proxy

		[Reactive]
		public ProxyType ProxyType { get; set; } = ProxyType.Socks5;

		[Reactive]
		public string ProxyServer { get; set; } = @"127.0.0.1:1080";

		[Reactive]
		public string? ProxyUser { get; set; }

		[Reactive]
		public string? ProxyPassword { get; set; }

		#endregion

		public MainWindowViewModel()
		{
			Result3489 = new ClassicStunResult
			{
				LocalEndPoint = new IPEndPoint(IPAddress.Any, 0)
			};
			Result5389 = new StunResult5389
			{
				LocalEndPoint = new IPEndPoint(IPAddress.Any, 0)
			};

			LoadStunServer();
			List.Connect()
				.DistinctValues(x => x)
				.ObserveOnDispatcher()
				.Bind(StunServers)
				.Subscribe();
			TestClassicNatType = ReactiveCommand.CreateFromTask(TestClassicNatTypeImpl);
			DiscoveryNatType = ReactiveCommand.CreateFromTask(DiscoveryNatTypeImpl);
		}

		private async void LoadStunServer()
		{
			foreach (var server in _defaultServers)
			{
				List.Add(server);
			}
			StunServer = _defaultServers.First();

			const string path = @"stun.txt";

			if (!File.Exists(path))
			{
				return;
			}

			using var sw = new StreamReader(path);
			string line;
			var stun = new StunServer();
			while ((line = await sw.ReadLineAsync()) != null)
			{
				if (!string.IsNullOrWhiteSpace(line) && stun.Parse(line))
				{
					List.Add(stun.ToString());
				}
			}
		}

		private async Task TestClassicNatTypeImpl(CancellationToken token)
		{
			try
			{
				var server = new StunServer();
				if (server.Parse(StunServer))
				{
					using var proxy = ProxyFactory.CreateProxy(
						ProxyType,
						Result3489.LocalEndPoint,
						NetUtils.ParseEndpoint(ProxyServer),
						ProxyUser, ProxyPassword
						);

					using var client = new StunClient3489(server.Hostname, server.Port, Result3489.LocalEndPoint, proxy);

					Result3489 = client.Status;
					await client.Query3489Async();
					//TODO
				}
				else
				{
					throw new Exception(@"Wrong STUN Server!");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, nameof(NatTypeTester), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async Task DiscoveryNatTypeImpl(CancellationToken token)
		{
			try
			{
				var server = new StunServer();
				if (server.Parse(StunServer))
				{
					using var proxy = ProxyFactory.CreateProxy(
						ProxyType,
						Result5389.LocalEndPoint,
						NetUtils.ParseEndpoint(ProxyServer),
						ProxyUser, ProxyPassword
						);

					using var client = new StunClient5389UDP(server.Hostname, server.Port, Result5389.LocalEndPoint, proxy);

					Result5389 = client.Status;
					await client.QueryAsync();
					//TODO
				}
				else
				{
					throw new Exception(@"Wrong STUN Server!");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, nameof(NatTypeTester), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
