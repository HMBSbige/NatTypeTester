namespace NatTypeTester.Configuration;

public sealed class AppConfigManager(IOptions<AppConfig> options, string configPath) : IAppConfigManager, IDisposable
{
	private static readonly JsonNode DefaultNode = JsonSerializer.SerializeToNode(new AppConfig(), AppConfigJsonContext.Default.AppConfig)!;

	private readonly SemaphoreSlim _saveLock = new(1, 1);

	private long _saveVersion;

	private readonly AppConfig _config = options.Value;

	public void Dispose()
	{
		_saveLock.Dispose();
	}

	public ValueTask<AppConfig> GetAsync(CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(_config);
	}

	public async ValueTask UpdateAsync(Action<AppConfig> update, CancellationToken cancellationToken = default)
	{
		long version;

		await _saveLock.WaitAsync(cancellationToken);
		try
		{
			update(_config);
			version = ++_saveVersion;
		}
		finally
		{
			_saveLock.Release();
		}

		await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);

		await _saveLock.WaitAsync(cancellationToken);
		try
		{
			if (_saveVersion != version)
			{
				return;
			}

			await SaveCoreAsync(_config, cancellationToken);
		}
		finally
		{
			_saveLock.Release();
		}
	}

	private async ValueTask SaveCoreAsync(AppConfig config, CancellationToken cancellationToken)
	{
		if (Path.GetDirectoryName(configPath) is { } directory)
		{
			Directory.CreateDirectory(directory);
		}

		JsonObject node = JsonSerializer.SerializeToNode(config, AppConfigJsonContext.Default.AppConfig)!.AsObject();

		foreach ((string key, JsonNode? value) in node.ToArray())
		{
			if (JsonNode.DeepEquals(Normalize(value), Normalize(DefaultNode[key])))
			{
				node.Remove(key);
			}
		}

		string tempPath = configPath + ".tmp";

		await using (FileStream stream = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
		{
			await JsonSerializer.SerializeAsync(stream, node, AppConfigJsonContext.Default.JsonObject, cancellationToken);
		}

		if (File.Exists(configPath))
		{
			string bakPath = configPath + ".bak";
			File.Replace(tempPath, configPath, bakPath);
		}
		else
		{
			File.Move(tempPath, configPath);
		}

		return;

		static JsonNode? Normalize(JsonNode? node)
		{
			return node?.GetValueKind() is JsonValueKind.String && string.IsNullOrEmpty(node.GetValue<string>()) ? null : node;
		}
	}
}
