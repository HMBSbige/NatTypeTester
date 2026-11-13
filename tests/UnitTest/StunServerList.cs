using TUnit.Core.Interfaces;

namespace UnitTest;

public sealed class StunServerList : IAsyncInitializer
{
	private static readonly HttpClient Client = new();

	public IReadOnlyList<string> Hosts { get; private set; } = [];

	public async Task InitializeAsync()
	{
		const string url = "https://raw.githubusercontent.com/pradt2/always-online-stun/master/valid_hosts_tcp.txt";
		CancellationToken cancellationToken = TestSessionContext.Current?.SessionCancellationToken ?? CancellationToken.None;
		string listRaw = await Client.GetStringAsync(url, cancellationToken);
		Hosts = Array.AsReadOnly(listRaw.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
	}
}
