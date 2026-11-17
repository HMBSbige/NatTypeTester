using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using System.Net;

namespace STUN.Client;

/// <summary>
/// A UDP-based STUN client implementing RFC 5389/5780 NAT behavior discovery,
/// supporting binding, mapping behavior, and filtering behavior tests.
/// </summary>
public class StunClient5389UDP : IStunClient5389, IUdpStunClient, IAsyncDisposable
{
	/// <inheritdoc />
	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3);

	private readonly IPEndPoint _remoteEndPoint;

	private readonly IUdpProxy _proxy;

	private readonly bool _ownedProxy;

	/// <inheritdoc />
	public StunResult5389 State { get; private set; } = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="StunClient5389UDP"/> class.
	/// </summary>
	/// <param name="server">The STUN server endpoint to query.</param>
	/// <param name="local">The local endpoint to bind to.</param>
	/// <param name="proxy">An optional UDP proxy for sending and receiving STUN messages.</param>
	/// <param name="ownedProxy">Whether this client owns and should dispose the proxy.</param>
	public StunClient5389UDP(IPEndPoint server, IPEndPoint local, IUdpProxy? proxy = default, bool ownedProxy = true)
	{
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

	private ValueTask<StunResponse?> RequestAsync(StunMessage5389 sendMessage, IPEndPoint remote, CancellationToken cancellationToken)
	{
		return _proxy.RequestAsync(sendMessage, remote, ReceiveTimeout, cancellationToken);
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
