namespace NatTypeTester.Domain.Configuration;

public interface IAppConfigManager
{
	ValueTask<AppConfig> GetAsync(CancellationToken cancellationToken = default);

	ValueTask SaveAsync(AppConfig config, CancellationToken cancellationToken = default);

	ValueTask UpdateAsync(Action<AppConfig> update, CancellationToken cancellationToken = default);
}
