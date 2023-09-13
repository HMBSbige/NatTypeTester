using Microsoft;
using STUN.Enums;
using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using STUN.Utils;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace STUN.Client;

/// <summary>
/// https://tools.ietf.org/html/rfc5389#section-7.2.1
/// https://tools.ietf.org/html/rfc5780#section-4.2
/// </summary>
public class StunClient5389UDP : IStunClient5389, IUdpStunClient
{
	public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3);

	private readonly IPEndPoint _remoteEndPoint;

	private readonly IUdpProxy _proxy;

	public StunResult5389 State { get; private set; } = new();

	public StunClient5389UDP(IPEndPoint server, IPEndPoint local, IUdpProxy? proxy = default)
	{
		Requires.NotNull(server, nameof(server));
		Requires.NotNull(local, nameof(local));

		_proxy = proxy ?? new NoneUdpProxy(local);

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
		State = new StunResult5389();

		await FilteringBehaviorTestBaseAsync(cancellationToken);
		if (State.BindingTestResult is not BindingTestResult.Success
			|| State.FilteringBehavior is FilteringBehavior.UnsupportedServer
		   )
		{
			return;
		}

		if (Equals(State.PublicEndPoint, State.LocalEndPoint))
		{
			State.MappingBehavior = MappingBehavior.Direct;
			return;
		}

		// MappingBehaviorTest test II
		StunResult5389 result2 = await MappingBehaviorTestBase2Async(cancellationToken);
		if (State.MappingBehavior is not MappingBehavior.Unknown)
		{
			return;
		}

		// MappingBehaviorTest test III
		await MappingBehaviorTestBase3Async(result2, cancellationToken);
	}

	public async ValueTask<StunResult5389> BindingTestAsync(CancellationToken cancellationToken = default)
	{
		return await BindingTestBaseAsync(_remoteEndPoint, cancellationToken);
	}

	protected virtual async ValueTask<StunResult5389> BindingTestBaseAsync(IPEndPoint remote, CancellationToken cancellationToken = default)
	{
		StunResult5389 result = new();
		StunMessage5389 test = new()
		{
			StunMessageType = StunMessageType.BindingRequest
		};
		StunResponse? response1 = await RequestAsync(test, remote, remote, cancellationToken);
		IPEndPoint? mappedAddress1 = response1?.Message.GetXorMappedAddressAttribute();
		IPEndPoint? otherAddress = response1?.Message.GetOtherAddressAttribute();

		if (response1 is null)
		{
			result.BindingTestResult = BindingTestResult.Fail;
		}
		else if (mappedAddress1 is null)
		{
			result.BindingTestResult = BindingTestResult.UnsupportedServer;
		}
		else
		{
			result.BindingTestResult = BindingTestResult.Success;
		}

		IPEndPoint? local = response1?.Local;

		result.LocalEndPoint = local;
		result.PublicEndPoint = mappedAddress1;
		result.OtherEndPoint = otherAddress;

		return result;
	}

	public async ValueTask MappingBehaviorTestAsync(CancellationToken cancellationToken = default)
	{
		State = new StunResult5389();

		// test I
		StunResult5389 bindingResult = await BindingTestAsync(cancellationToken);
		State = bindingResult with { };
		if (State.BindingTestResult is not BindingTestResult.Success)
		{
			return;
		}

		if (!HasValidOtherAddress(State.OtherEndPoint))
		{
			State.MappingBehavior = MappingBehavior.UnsupportedServer;
			return;
		}

		if (Equals(State.PublicEndPoint, State.LocalEndPoint))
		{
			State.MappingBehavior = MappingBehavior.Direct; // or Endpoint-Independent
			return;
		}

		// test II
		StunResult5389 result2 = await MappingBehaviorTestBase2Async(cancellationToken);
		if (State.MappingBehavior is not MappingBehavior.Unknown)
		{
			return;
		}

		// test III
		await MappingBehaviorTestBase3Async(result2, cancellationToken);
	}

	private async ValueTask<StunResult5389> MappingBehaviorTestBase2Async(CancellationToken cancellationToken)
	{
		Verify.Operation(State.OtherEndPoint is not null, @"OTHER-ADDRESS is not returned");

		StunResult5389 result2 = await BindingTestBaseAsync(new IPEndPoint(State.OtherEndPoint.Address, _remoteEndPoint.Port), cancellationToken);

		if (result2.BindingTestResult is not BindingTestResult.Success)
		{
			State.MappingBehavior = MappingBehavior.Fail;
		}
		else if (Equals(result2.PublicEndPoint, State.PublicEndPoint))
		{
			State.MappingBehavior = MappingBehavior.EndpointIndependent;
		}

		return result2;
	}

	private async ValueTask MappingBehaviorTestBase3Async(StunResult5389 result2, CancellationToken cancellationToken)
	{
		Verify.Operation(State.OtherEndPoint is not null, @"OTHER-ADDRESS is not returned");

		StunResult5389 result3 = await BindingTestBaseAsync(State.OtherEndPoint, cancellationToken);
		if (result3.BindingTestResult is not BindingTestResult.Success)
		{
			State.MappingBehavior = MappingBehavior.Fail;
			return;
		}

		State.MappingBehavior = Equals(result3.PublicEndPoint, result2.PublicEndPoint) ? MappingBehavior.AddressDependent : MappingBehavior.AddressAndPortDependent;
	}

	public async ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default)
	{
		State = new StunResult5389();
		await FilteringBehaviorTestBaseAsync(cancellationToken);
	}

	private async ValueTask FilteringBehaviorTestBaseAsync(CancellationToken cancellationToken)
	{
		// test I
		StunResult5389 bindingResult = await BindingTestAsync(cancellationToken);
		State = bindingResult with { };
		if (State.BindingTestResult is not BindingTestResult.Success)
		{
			return;
		}

		if (!HasValidOtherAddress(State.OtherEndPoint))
		{
			State.FilteringBehavior = FilteringBehavior.UnsupportedServer;
			return;
		}

		// test II
		StunResponse? response2 = await FilteringBehaviorTest2Async(cancellationToken);
		if (response2 is not null)
		{
			State.FilteringBehavior = Equals(response2.Remote, State.OtherEndPoint) ? FilteringBehavior.EndpointIndependent : FilteringBehavior.UnsupportedServer;
			return;
		}

		// test III
		StunResponse? response3 = await FilteringBehaviorTest3Async(cancellationToken);
		if (response3 is null)
		{
			State.FilteringBehavior = FilteringBehavior.AddressAndPortDependent;
			return;
		}

		if (Equals(response3.Remote.Address, _remoteEndPoint.Address) && response3.Remote.Port != _remoteEndPoint.Port)
		{
			State.FilteringBehavior = FilteringBehavior.AddressDependent;
		}
		else
		{
			State.FilteringBehavior = FilteringBehavior.UnsupportedServer;
		}
	}

	protected virtual async ValueTask<StunResponse?> FilteringBehaviorTest2Async(CancellationToken cancellationToken = default)
	{
		Assumes.NotNull(State.OtherEndPoint);

		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			Attributes = new[] { AttributeExtensions.BuildChangeRequest(true, true) }
		};
		return await RequestAsync(message, _remoteEndPoint, State.OtherEndPoint, cancellationToken);
	}

	protected virtual async ValueTask<StunResponse?> FilteringBehaviorTest3Async(CancellationToken cancellationToken = default)
	{
		Assumes.NotNull(State.OtherEndPoint);

		StunMessage5389 message = new()
		{
			StunMessageType = StunMessageType.BindingRequest,
			Attributes = new[] { AttributeExtensions.BuildChangeRequest(false, true) }
		};
		return await RequestAsync(message, _remoteEndPoint, _remoteEndPoint, cancellationToken);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool HasValidOtherAddress([NotNullWhen(true)] IPEndPoint? other)
	{
		return other is not null && !Equals(other.Address, _remoteEndPoint.Address) && other.Port != _remoteEndPoint.Port;
	}

	private async ValueTask<StunResponse?> RequestAsync(StunMessage5389 sendMessage, IPEndPoint remote, IPEndPoint receive, CancellationToken cancellationToken)
	{
		try
		{
			using IMemoryOwner<byte> memoryOwner = MemoryPool<byte>.Shared.Rent(0x10000);
			Memory<byte> buffer = memoryOwner.Memory;
			int length = sendMessage.WriteTo(buffer.Span);

			await _proxy.SendToAsync(buffer[..length], SocketFlags.None, remote, cancellationToken);

			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(ReceiveTimeout);
			SocketReceiveMessageFromResult r = await _proxy.ReceiveMessageFromAsync(buffer, SocketFlags.None, receive, cts.Token);

			StunMessage5389 message = new();
			if (message.TryParse(buffer[..r.ReceivedBytes]) && message.IsSameTransaction(sendMessage))
			{
				return new StunResponse(message, (IPEndPoint)r.RemoteEndPoint, new IPEndPoint(r.PacketInformation.Address, ((IPEndPoint)_proxy.Client.LocalEndPoint!).Port));
			}
		}
		catch (OperationCanceledException ex)
		{
			Debug.WriteLine(ex);
		}
		return default;
	}

	public void Dispose()
	{
		_proxy.Dispose();

		GC.SuppressFinalize(this);
	}
}
