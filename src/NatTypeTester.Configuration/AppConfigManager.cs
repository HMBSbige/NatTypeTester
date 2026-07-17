namespace NatTypeTester.Configuration;

public sealed class AppConfigManager(IOptions<AppConfigStorageOptions> storageOptions) : IAppConfigManager
{
	private static readonly JsonObject DefaultNode = SerializeToJsonObject(AppConfig.CreateDefault());

	private readonly string _configPath = storageOptions.Value.FilePath;
	private readonly SemaphoreSlim _saveLock = new(1, 1);
	private long _saveVersion;
	private AppConfig? _config;

	public async ValueTask<AppConfig> GetAsync(CancellationToken cancellationToken = default)
	{
		await _saveLock.WaitAsync(cancellationToken);

		try
		{
			return await GetOrLoadCoreAsync(cancellationToken);
		}
		finally
		{
			_saveLock.Release();
		}
	}

	public async ValueTask UpdateAsync(Action<AppConfig> update, CancellationToken cancellationToken = default)
	{
		AppConfig config;
		long version;

		await _saveLock.WaitAsync(cancellationToken);

		try
		{
			config = await GetOrLoadCoreAsync(cancellationToken);
			update(config);
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

			await SaveCoreAsync(config, cancellationToken);
		}
		finally
		{
			_saveLock.Release();
		}
	}

	private async ValueTask<AppConfig> GetOrLoadCoreAsync(CancellationToken cancellationToken)
	{
		return _config ??= await LoadCoreAsync(cancellationToken);
	}

	private async ValueTask<AppConfig> LoadCoreAsync(CancellationToken cancellationToken)
	{
		AppConfig config;

		try
		{
			if (!File.Exists(_configPath))
			{
				config = new AppConfig();
			}
			else
			{
				await using FileStream stream = File.OpenRead(_configPath);
				config = await JsonSerializer.DeserializeAsync
				(
					stream,
					AppConfigJsonContext.Default.AppConfig,
					cancellationToken
				) ?? new AppConfig();
			}
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
		{
			throw;
		}
		catch
		{
			config = new AppConfig();
		}

		config.ApplyDefaults();

		return config;
	}

	private async ValueTask SaveCoreAsync(AppConfig config, CancellationToken cancellationToken)
	{
		if (Path.GetDirectoryName(_configPath) is { } directory)
		{
			Directory.CreateDirectory(directory);
		}

		JsonObject node = SerializeToJsonObject(config);

		foreach ((string key, JsonNode? value) in node.ToArray())
		{
			if (JsonNode.DeepEquals(Normalize(value), Normalize(DefaultNode[key])))
			{
				node.Remove(key);
			}
		}

		string tempPath = _configPath + ".tmp";

		await using (FileStream stream = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
		{
			await JsonSerializer.SerializeAsync(stream, node, AppConfigJsonContext.Default.JsonObject, cancellationToken);
		}

		if (File.Exists(_configPath))
		{
			string bakPath = _configPath + ".bak";
			File.Replace(tempPath, _configPath, bakPath);
		}
		else
		{
			File.Move(tempPath, _configPath);
		}

		return;

		static JsonNode? Normalize(JsonNode? node)
		{
			return node?.GetValueKind() is JsonValueKind.String && string.IsNullOrEmpty(node.GetValue<string>()) ? null : node;
		}
	}

	private static JsonObject SerializeToJsonObject(AppConfig config)
	{
		return JsonSerializer.SerializeToNode(config, AppConfigJsonContext.Default.AppConfig) as JsonObject
				?? throw new InvalidOperationException(@"Serialized app config is not a JSON object.");
	}
}
