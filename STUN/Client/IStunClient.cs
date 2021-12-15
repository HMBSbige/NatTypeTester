namespace STUN.Client;

public interface IStunClient : IDisposable
{
	ValueTask ConnectProxyAsync(CancellationToken cancellationToken = default);
	ValueTask CloseProxyAsync(CancellationToken cancellationToken = default);
	ValueTask QueryAsync(CancellationToken cancellationToken = default);
}
