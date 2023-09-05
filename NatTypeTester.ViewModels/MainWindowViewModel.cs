using DynamicData;
using DynamicData.Binding;
using Microsoft.VisualStudio.Threading;
using NatTypeTester.Models;
using ReactiveUI;
using STUN;
using System.Reactive.Linq;
using Volo.Abp.DependencyInjection;

namespace NatTypeTester.ViewModels;

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
		@"stunserver.stunprotocol.org",
		@"stun.syncthing.net",
		@"stun.qq.com",
		@"stun.miwifi.com"
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
		foreach (string? server in _defaultServers)
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

			foreach (string? line in File.ReadLines(path))
			{
				if (!string.IsNullOrWhiteSpace(line) && StunServer.TryParse(line, out StunServer? stun))
				{
					List.Add(stun.ToString());
				}
			}
		}).Forget();
	}
}
