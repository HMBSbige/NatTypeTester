using Socks5.Enums;
using Socks5.Exceptions;
using STUN.Enums;
using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;

namespace STUN.Client;

/// <summary>
/// A TCP-based STUN client implementing RFC 5389/5780 NAT behavior discovery.
/// Filtering behavior tests are not supported over TCP.
/// </summary>
/// <remarks>
/// A request to a STUN server that refuses the connection gets no response, as one to a closed UDP port does:
/// either a direct connection, with or without TLS, is refused, or a SOCKS5 proxy replies that the server refused it.
/// Other connection failures, such as an unreachable proxy or a failed TLS handshake, are thrown.
/// </remarks>
public class StunClient5389TCP : IStunClient5389
{
	/// <summary>
	/// Gets or sets the timeout duration for establishing TCP connections to the STUN server.
	/// </summary>
	public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(3);

	/// <summary>
	/// Gets or sets the timeout duration for each STUN request once its TCP connection is established,
	/// covering the time from sending the request until the response is received.
	/// </summary>
	/// <remarks>
	/// A request that times out is treated as having received no response.
	/// Establishing the connection, including any proxy or TLS handshake, is limited separately by <see cref="ConnectTimeout"/>.
	/// </remarks>
	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3);

	private readonly IPEndPoint _remoteEndPoint;
	private IPEndPoint _lastLocalEndPoint;

	private readonly ITcpProxy _proxy;
	private readonly bool _ownedProxy;

	/// <inheritdoc />
	public StunResult5389 State { get; private set; } = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="StunClient5389TCP"/> class.
	/// </summary>
	/// <param name="server">The STUN server endpoint to query.</param>
	/// <param name="local">The local endpoint to bind to.</param>
	/// <param name="proxy">An optional TCP proxy for connecting to the STUN server.</param>
	/// <param name="ownedProxy">Whether this client owns and should dispose the proxy.</param>
	public StunClient5389TCP(IPEndPoint server, IPEndPoint local, ITcpProxy? proxy = default, bool ownedProxy = true)
	{
		_proxy = proxy ?? new DirectTcpProxy();
		_ownedProxy = ownedProxy;

		_remoteEndPoint = server;

		_lastLocalEndPoint = local;
		State.LocalEndPoint = local;
	}

	/// <inheritdoc />
	public async ValueTask QueryAsync(CancellationToken cancellationToken = default)
	{
		await MappingBehaviorTestAsync(cancellationToken);
		State.FilteringBehavior = FilteringBehavior.None;
	}

	/// <inheritdoc />
	public async ValueTask MappingBehaviorTestAsync(CancellationToken cancellationToken = default)
	{
		Stun5389NatBehaviorDiscovery session = new(_remoteEndPoint);
		State = session.Result;
		StunDiscoveryAction? action = session.CreateMappingBehaviorTest();

		while (action is not null)
		{
			StunResponse? response = await RequestAsync(action.Message, action.SendTo, cancellationToken);
			action = session.GotResponse(response);
			State = session.Result;
		}
	}

	/// <inheritdoc />
	/// <exception cref="NotSupportedException">Always thrown because filtering tests are only applicable to UDP.</exception>
	public ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException(@"Filtering test applies only to UDP.");
	}

	/// <inheritdoc />
	public async ValueTask<StunResult5389> BindingTestAsync(CancellationToken cancellationToken = default)
	{
		Stun5389NatBehaviorDiscovery session = new(_remoteEndPoint);
		StunDiscoveryAction? action = session.CreateBindingTest();

		while (action is not null)
		{
			StunResponse? response = await RequestAsync(action.Message, action.SendTo, cancellationToken);
			action = session.GotResponse(response);
		}

		return session.Result;
	}

	private async ValueTask<StunResponse?> RequestAsync(StunMessage5389 sendMessage, IPEndPoint remote, CancellationToken cancellationToken)
	{
		try
		{
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(ConnectTimeout);
			IDuplexPipe pipe = await _proxy.ConnectAsync(_lastLocalEndPoint, remote, cts.Token);

			try
			{
				// Calling CancelAfter again restarts the timer. Sending and the whole read loop share this one deadline,
				// so incoming data that never parses cannot extend it
				cts.CancelAfter(ReceiveTimeout);

				int length = sendMessage.WriteTo(pipe.Output.GetSpan(sendMessage.Length));

				pipe.Output.Advance(length);
				await pipe.Output.FlushAsync(cts.Token);

				StunMessage5389 message = new();
				bool success = await ReadPipeAsync(message, pipe.Input, cts.Token);

				if (success && message.IsSameTransaction(sendMessage) && _proxy.CurrentLocalEndPoint is { } local)
				{
					_lastLocalEndPoint = local;
					return new StunResponse(message, remote, local);
				}
			}
			finally
			{
				await _proxy.CloseAsync(cancellationToken);
			}
		}
		catch (OperationCanceledException ex)
		{
			Debug.WriteLine(ex);
		}
		catch (Exception ex) when (IsRefusedByServer(ex))
		{
			Debug.WriteLine(ex);
		}

		return default;

		static async ValueTask<bool> ReadPipeAsync(StunMessage5389 message, PipeReader reader, CancellationToken cancellationToken)
		{
			try
			{
				while (true)
				{
					cancellationToken.ThrowIfCancellationRequested();

					ReadResult result = await reader.ReadAsync(cancellationToken);
					ReadOnlySequence<byte> buffer = result.Buffer;

					try
					{
						if (message.TryParse(ref buffer))
						{
							return true;
						}

						if (result.IsCompleted)
						{
							break;
						}
					}
					finally
					{
						reader.AdvanceTo(buffer.Start, buffer.End);
					}
				}

				return false;
			}
			finally
			{
				await reader.CompleteAsync();
			}
		}
	}

	private bool IsRefusedByServer(Exception ex)
	{
		return ex switch
		{
			// Only a direct connection reaches the server itself; through SOCKS5, this is the proxy refusing
			SocketException { SocketErrorCode: SocketError.ConnectionRefused } => _proxy is DirectTcpProxy,
			// The SOCKS5 proxy reached the server, which refused the connection
			Socks5ProtocolErrorException { Socks5Reply: Socks5Reply.ConnectionRefused } => true,
			_ => false
		};
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (_ownedProxy)
		{
			_proxy.Dispose();
		}

		GC.SuppressFinalize(this);
	}
}
