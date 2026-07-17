namespace NatTypeTester.Configuration;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(JsonObject))]
[JsonSourceGenerationOptions
(
	WriteIndented = true,
	IndentCharacter = '\t',
	IndentSize = 1,
	PropertyNameCaseInsensitive = true,
	ReadCommentHandling = JsonCommentHandling.Skip,
	AllowTrailingCommas = true
)]
internal partial class AppConfigJsonContext : JsonSerializerContext;
