namespace STUN.Client;

/// <summary>
/// Represents a STUN client capable of performing NAT discovery queries.
/// </summary>
public interface IStunClient : IDisposable
{
	/// <summary>
	/// Performs a full STUN query to discover NAT characteristics.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous query operation.</returns>
	ValueTask QueryAsync(CancellationToken cancellationToken = default);
}
