namespace NatTypeTester.Configuration;

public sealed class AppConfigManager(IOptions<AppConfig> options, string configPath) : IAppConfigManager
{
	private static readonly JsonNode DefaultNode = JsonSerializer.SerializeToNode(new AppConfig(), AppConfigJsonContext.Default.AppConfig)!;

	private AppConfig _config = options.Value;

	public ValueTask<AppConfig> GetAsync(CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(_config);
	}

	public async ValueTask SaveAsync(AppConfig config, CancellationToken cancellationToken = default)
	{
		_config = config;

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

	public async ValueTask UpdateAsync(Action<AppConfig> update, CancellationToken cancellationToken = default)
	{
		AppConfig config = await GetAsync(cancellationToken);
		update(config);
		await SaveAsync(config, cancellationToken);
	}
}
