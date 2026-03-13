using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace STUN.Client;

/// <summary>
/// A UDP-based STUN client implementing the classic NAT type discovery algorithm defined in RFC 3489.
/// </summary>
public class StunClient3489 : IUdpStunClient, IAsyncDisposable
{
	/// <inheritdoc />
	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3);

	private readonly IPEndPoint _remoteEndPoint;

	private readonly IUdpProxy _proxy;
	private readonly bool _ownedProxy;

	/// <summary>
	/// Gets the classic STUN result containing the discovered NAT type and endpoints.
	/// </summary>
	public ClassicStunResult State { get; private set; } = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="StunClient3489"/> class.
	/// </summary>
	/// <param name="server">The STUN server endpoint to query.</param>
	/// <param name="local">The local endpoint to bind to.</param>
	/// <param name="proxy">An optional UDP proxy for sending and receiving STUN messages.</param>
	/// <param name="ownedProxy">Whether this client owns and should dispose the proxy.</param>
	public StunClient3489(IPEndPoint server, IPEndPoint local, IUdpProxy? proxy = null, bool ownedProxy = true)
	{
		ArgumentNullException.ThrowIfNull(server);
		ArgumentNullException.ThrowIfNull(local);

		_proxy = proxy ?? new NoneUdpProxy(local);
		_ownedProxy = ownedProxy;

		_remoteEndPoint = server;

		State.LocalEndPoint = local;
	}

	/// <inheritdoc />
	public async ValueTask ConnectProxyAsync(CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(ReceiveTimeout);

		await _proxy.ConnectAsync(cts.Token);
	}

	/// <inheritdoc />
	public async ValueTask CloseProxyAsync(CancellationToken cancellationToken = default)
	{
		await _proxy.CloseAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async ValueTask QueryAsync(CancellationToken cancellationToken = default)
	{
		Stun3489NatTypeDiscovery session = new(_remoteEndPoint);
		State = session.Result;
		StunDiscoveryAction? action = session.CreateQuery();

		while (action is not null)
		{
			StunResponse? response = await RequestAsync(action.Message, action.SendTo, cancellationToken);
			action = session.GotResponse(response);
			State = session.Result;
		}
	}

	private async ValueTask<StunResponse?> RequestAsync(StunMessage5389 sendMessage, IPEndPoint remote, CancellationToken cancellationToken)
	{
		try
		{
			using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(0x10000);
			Memory<byte> buffer = memoryOwner.Memory;
			int length = sendMessage.WriteTo(buffer.Span);

			await _proxy.SendToAsync(buffer[..length], SocketFlags.None, remote, cancellationToken);

			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(ReceiveTimeout);
			// remote 仅用于提供 AddressFamily，UDP 下不过滤来源
			SocketReceiveMessageFromResult r = await _proxy.ReceiveMessageFromAsync(buffer, SocketFlags.None, remote, cts.Token);

			StunMessage5389 message = new();

			if (message.TryParse(buffer[..r.ReceivedBytes]) && message.IsSameTransaction(sendMessage))
			{
				return new StunResponse(message, (IPEndPoint)r.RemoteEndPoint, new IPEndPoint(r.PacketInformation.Address, GetClientLocalEndPoint().Port));
			}
		}
		catch (OperationCanceledException ex)
		{
			Debug.WriteLine(ex);
		}

		return default;
	}

	private IPEndPoint GetClientLocalEndPoint()
	{
		return _proxy.Client.LocalEndPoint as IPEndPoint
				?? throw new InvalidOperationException(@"UDP client local endpoint is unavailable.");
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_ownedProxy)
		{
			await _proxy.DisposeAsync();
		}

		GC.SuppressFinalize(this);
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
