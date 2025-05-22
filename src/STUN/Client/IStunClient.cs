namespace STUN.Client;

public interface IStunClient : IDisposable
{
	ValueTask QueryAsync(CancellationToken cancellationToken = default);
}
