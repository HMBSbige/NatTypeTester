using DynamicData;
using DynamicData.Binding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Threading;
using NatTypeTester.Models;
using ReactiveUI;
using STUN;
using System.Collections.Frozen;
using System.Reactive.Linq;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.ViewModels;

[ExposeServices(
	typeof(MainWindowViewModel),
	typeof(IScreen)
)]
public class MainWindowViewModel : ViewModelBase, IScreen
{
	public RoutingState Router => TransientCachedServiceProvider.GetRequiredService<RoutingState>();

	public Config Config => TransientCachedServiceProvider.GetRequiredService<Config>();

	private static readonly FrozenSet<string> DefaultServers =
	[
		@"stun.hot-chilli.net",
		@"stun.fitauto.ru",
		@"stun.internetcalls.com",
		@"stun.miwifi.com",
		@"stun.voip.aebc.com",
		@"stun.voipbuster.com",
		@"stun.voipstunt.com"
	];

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
		foreach (string? server in DefaultServers)
		{
			List.Add(server);
		}

		Config.StunServer = DefaultServers.First();

		Task.Run(() =>
		{
			const string path = @"stun.txt";

			if (!File.Exists(path))
			{
				return;
			}

			foreach (string line in File.ReadLines(path))
			{
				if (!string.IsNullOrWhiteSpace(line) && StunServer.TryParse(line, out StunServer? stun))
				{
					List.Add(stun.ToString());
				}
			}
		}).Forget();
	}
}
