using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace STUN.Client;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5389#section-7.2.1
/// https://datatracker.ietf.org/doc/html/rfc5780#section-4.2
/// </summary>
public class StunClient5389UDP : IStunClient5389, IUdpStunClient, IAsyncDisposable
{
	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3);

	private readonly IPEndPoint _remoteEndPoint;

	private readonly IUdpProxy _proxy;

	private readonly bool _ownedProxy;

	public StunResult5389 State { get; private set; } = new();

	public StunClient5389UDP(IPEndPoint server, IPEndPoint local, IUdpProxy? proxy = default, bool ownedProxy = true)
	{
		ArgumentNullException.ThrowIfNull(server);
		ArgumentNullException.ThrowIfNull(local);

		_proxy = proxy ?? new NoneUdpProxy(local);
		_ownedProxy = ownedProxy;

		_remoteEndPoint = server;

		State.LocalEndPoint = local;
	}

	public async ValueTask ConnectProxyAsync(CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		cts.CancelAfter(ReceiveTimeout);

		await _proxy.ConnectAsync(cts.Token);
	}

	public async ValueTask CloseProxyAsync(CancellationToken cancellationToken = default)
	{
		await _proxy.CloseAsync(cancellationToken);
	}

	public async ValueTask QueryAsync(CancellationToken cancellationToken = default)
	{
		Stun5389NatBehaviorDiscovery session = new(_remoteEndPoint);
		State = session.Result;
		StunDiscoveryAction? action = session.CreateQuery();

		while (action is not null)
		{
			StunResponse? response = await RequestAsync(action.Message, action.SendTo, cancellationToken);
			action = session.GotResponse(response);
			State = session.Result;
		}
	}

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

	public async ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default)
	{
		Stun5389NatBehaviorDiscovery session = new(_remoteEndPoint);
		State = session.Result;
		StunDiscoveryAction? action = session.CreateFilteringBehaviorTest();

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

	public async ValueTask DisposeAsync()
	{
		if (_ownedProxy)
		{
			await _proxy.DisposeAsync();
		}

		GC.SuppressFinalize(this);
	}

	public void Dispose()
	{
		if (_ownedProxy)
		{
			_proxy.Dispose();
		}

		GC.SuppressFinalize(this);
	}
}
