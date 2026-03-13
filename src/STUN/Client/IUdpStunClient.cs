namespace STUN.Client;

/// <summary>
/// Represents a UDP-based STUN client with proxy connection support.
/// </summary>
public interface IUdpStunClient : IStunClient
{
	/// <summary>
	/// Gets or sets the timeout duration for receiving STUN responses.
	/// </summary>
	TimeSpan ReceiveTimeout { get; set; }

	/// <summary>
	/// Connects the underlying UDP proxy for sending and receiving STUN messages.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous connect operation.</returns>
	ValueTask ConnectProxyAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Closes the underlying UDP proxy connection.
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous close operation.</returns>
	ValueTask CloseProxyAsync(CancellationToken cancellationToken = default);
}
