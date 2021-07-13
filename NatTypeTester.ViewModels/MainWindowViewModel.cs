using DynamicData;
using DynamicData.Binding;
using NatTypeTester.Models;
using ReactiveUI;
using STUN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.ViewModels
{
	[ExposeServices(
		typeof(MainWindowViewModel),
		typeof(IScreen)
	)]
	public class MainWindowViewModel : ViewModelBase, IScreen
	{
		public RoutingState Router { get; } = new();

		public Config Config { get; }

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

		public MainWindowViewModel(Config config)
		{
			Config = config;

			LoadStunServer();
			List.Connect()
				.DistinctValues(x => x)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Bind(StunServers)
				.Subscribe();
		}

		private async void LoadStunServer()
		{
			foreach (var server in _defaultServers)
			{
				List.Add(server);
			}
			Config.StunServer = _defaultServers.First();

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
	}
}
