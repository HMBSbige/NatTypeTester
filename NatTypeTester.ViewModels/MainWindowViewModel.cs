using DynamicData;
using DynamicData.Binding;
using Microsoft.VisualStudio.Threading;
using NatTypeTester.Models;
using ReactiveUI;
using STUN;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.ViewModels
{
	[ExposeServices(
		typeof(MainWindowViewModel),
		typeof(IScreen)
	)]
	public class MainWindowViewModel : ViewModelBase, IScreen
	{
		public RoutingState Router => LazyServiceProvider.LazyGetRequiredService<RoutingState>();

		public Config Config => LazyServiceProvider.LazyGetRequiredService<Config>();

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

		public MainWindowViewModel()
		{
			List.Connect()
				.DistinctValues(x => x)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Bind(StunServers)
				.Subscribe();
		}

		public void LoadStunServer()
		{
			foreach (var server in _defaultServers)
			{
				List.Add(server);
			}

			Config.StunServer = _defaultServers.First();

			Task.Run(() =>
			{
				const string path = @"stun.txt";

				if (!File.Exists(path))
				{
					return;
				}

				foreach (var line in File.ReadLines(path))
				{
					if (!string.IsNullOrWhiteSpace(line) && StunServer.TryParse(line, out var stun))
					{
						List.Add(stun.ToString());
					}
				}
			}).Forget();
		}
	}
}
