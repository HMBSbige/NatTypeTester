using DynamicData;
using DynamicData.Binding;
using NatTypeTester.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using STUN.Client;
using STUN.Enums;
using STUN.Proxy;
using STUN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;

namespace NatTypeTester.ViewModels
{
	public class MainWindowViewModel : ReactiveObject
	{
		#region RFC3489

		[Reactive]
		public string? ClassicNatType { get; set; }

		[Reactive]
		public string LocalEnd { get; set; } = NetUtils.DefaultLocalEnd;

		[Reactive]
		public string? PublicEnd { get; set; }

		public ReactiveCommand<Unit, Unit> TestClassicNatType { get; }

		#endregion

		#region RFC5780

		[Reactive]
		public string? BindingTest { get; set; }

		[Reactive]
		public string? MappingBehavior { get; set; }

		[Reactive]
		public string? FilteringBehavior { get; set; }

		[Reactive]
		public string? LocalAddress { get; set; }

		[Reactive]
		public string? MappingAddress { get; set; }

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
			LoadStunServer();
			List.Connect()
				.DistinctValues(x => x)
				.ObserveOnDispatcher()
				.Bind(StunServers)
				.Subscribe();
			TestClassicNatType = ReactiveCommand.CreateFromObservable(TestClassicNatTypeImpl);
			DiscoveryNatType = ReactiveCommand.CreateFromObservable(DiscoveryNatTypeImpl);
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

		private IObservable<Unit> TestClassicNatTypeImpl()
		{
			return Observable.FromAsync(async () =>
			{
				try
				{
					var server = new StunServer();
					if (server.Parse(StunServer))
					{
						using var proxy = ProxyFactory.CreateProxy(
							ProxyType,
							NetUtils.ParseEndpoint(LocalEnd),
							NetUtils.ParseEndpoint(ProxyServer),
							ProxyUser, ProxyPassword
							);

						using var client = new StunClient3489(server.Hostname, server.Port, NetUtils.ParseEndpoint(LocalEnd), proxy);

						client.NatTypeChanged.ObserveOn(RxApp.MainThreadScheduler)
								.Subscribe(t => ClassicNatType = $@"{t}");
						client.PubChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(t => PublicEnd = $@"{t}");
						client.LocalChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(t => LocalEnd = $@"{t}");
						await client.Query3489Async();
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
			}).SubscribeOn(RxApp.TaskpoolScheduler);
		}

		private IObservable<Unit> DiscoveryNatTypeImpl()
		{
			return Observable.FromAsync(async () =>
			{
				try
				{
					var server = new StunServer();
					if (server.Parse(StunServer))
					{
						using var proxy = ProxyFactory.CreateProxy(
							ProxyType,
							NetUtils.ParseEndpoint(LocalEnd),
							NetUtils.ParseEndpoint(ProxyServer),
							ProxyUser, ProxyPassword
							);

						using var client = new StunClient5389UDP(server.Hostname, server.Port, NetUtils.ParseEndpoint(LocalAddress), proxy);

						client.BindingTestResultChanged
								.ObserveOn(RxApp.MainThreadScheduler)
								.Subscribe(t => BindingTest = $@"{t}");

						client.MappingBehaviorChanged
								.ObserveOn(RxApp.MainThreadScheduler)
								.Subscribe(t => MappingBehavior = $@"{t}");

						client.FilteringBehaviorChanged
								.ObserveOn(RxApp.MainThreadScheduler)
								.Subscribe(t => FilteringBehavior = $@"{t}");

						client.PubChanged
								.ObserveOn(RxApp.MainThreadScheduler)
								.Subscribe(t => MappingAddress = $@"{t}");

						client.LocalChanged
								.ObserveOn(RxApp.MainThreadScheduler)
								.Subscribe(t => LocalAddress = $@"{t}");

						await client.QueryAsync();
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
			}).SubscribeOn(RxApp.TaskpoolScheduler);
		}
	}
}
