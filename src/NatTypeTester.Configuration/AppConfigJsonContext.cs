namespace NatTypeTester.Configuration;

[JsonSerializable(typeof(AppConfig))]
[JsonSerializable(typeof(JsonObject))]
[JsonSourceGenerationOptions(WriteIndented = true, IndentCharacter = '\t', IndentSize = 1)]
internal partial class AppConfigJsonContext : JsonSerializerContext;
