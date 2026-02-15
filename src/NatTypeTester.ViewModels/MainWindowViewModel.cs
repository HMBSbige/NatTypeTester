namespace NatTypeTester.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, ISingletonDependency
{
	public RFC3489ViewModel RFC3489ViewModel => TransientCachedServiceProvider.GetRequiredService<RFC3489ViewModel>();
	public RFC5780ViewModel RFC5780ViewModel => TransientCachedServiceProvider.GetRequiredService<RFC5780ViewModel>();
	public SettingsViewModel SettingsViewModel => TransientCachedServiceProvider.GetRequiredService<SettingsViewModel>();

	private static readonly List<string> DefaultServers =
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

	public IObservableCollection<string> StunServers { get; } = new ObservableCollectionExtended<string>();

	[Reactive]
	public partial string? CurrentCulture { get; set; }

	public MainWindowViewModel()
	{
		List.Connect()
			.DistinctValues(x => x)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Bind(StunServers)
			.Subscribe();

		Locator.Current.GetService<ObservableCultureService>()?
			.CultureChanged
			.Subscribe(_ => CurrentCulture = CultureInfo.CurrentCulture.DisplayName);
	}

	public void LoadStunServer()
	{
		foreach (string server in DefaultServers)
		{
			List.Add(server);
		}

		SettingsViewModel.StunServer = DefaultServers.First();

		Task.Run
		(() =>
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
			}
		);
	}
}
