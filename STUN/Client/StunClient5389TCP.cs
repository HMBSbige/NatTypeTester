using Microsoft;
using STUN.Enums;
using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using STUN.Utils;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net;

namespace STUN.Client;

public class StunClient5389TCP : IStunClient5389
{
	public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(3);

	private readonly IPEndPoint _remoteEndPoint;
	private IPEndPoint _lastLocalEndPoint;

	private readonly ITcpProxy _proxy;

	public StunResult5389 State { get; private set; } = new();

	public StunClient5389TCP(IPEndPoint server, IPEndPoint local, ITcpProxy? proxy = default)
	{
		Requires.NotNull(server, nameof(server));
		Requires.NotNull(local, nameof(local));

		_proxy = proxy ?? new DirectTcpProxy();

		_remoteEndPoint = server;

		_lastLocalEndPoint = local;
		State.LocalEndPoint = local;
	}

	public async ValueTask QueryAsync(CancellationToken cancellationToken = default)
	{
		await MappingBehaviorTestAsync(cancellationToken);
		State.FilteringBehavior = FilteringBehavior.None;
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
		StunResult5389 result2 = await MappingBehaviorTestBase2Async();
		if (State.MappingBehavior is not MappingBehavior.Unknown)
		{
			return;
		}

		// test III
		await MappingBehaviorTestBase3Async();

		return;

		bool HasValidOtherAddress([NotNullWhen(true)] IPEndPoint? other)
		{
			return other is not null && !Equals(other.Address, _remoteEndPoint.Address) && other.Port != _remoteEndPoint.Port;
		}

		async ValueTask<StunResult5389> MappingBehaviorTestBase2Async()
		{
			StunResult5389 result = await BindingTestBaseAsync(new IPEndPoint(State.OtherEndPoint.Address, _remoteEndPoint.Port), cancellationToken);

			if (result.BindingTestResult is not BindingTestResult.Success)
			{
				State.MappingBehavior = MappingBehavior.Fail;
			}
			else if (Equals(result.PublicEndPoint, State.PublicEndPoint))
			{
				State.MappingBehavior = MappingBehavior.EndpointIndependent;
			}
			return result;
		}

		async ValueTask MappingBehaviorTestBase3Async()
		{
			StunResult5389 result3 = await BindingTestBaseAsync(State.OtherEndPoint, cancellationToken);
			if (result3.BindingTestResult is not BindingTestResult.Success)
			{
				State.MappingBehavior = MappingBehavior.Fail;
				return;
			}

			State.MappingBehavior = Equals(result3.PublicEndPoint, result2.PublicEndPoint) ? MappingBehavior.AddressDependent : MappingBehavior.AddressAndPortDependent;
		}
	}

	public ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException(@"Filtering test applies only to UDP.");
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
		StunResponse? response1 = await RequestAsync(test, remote, cancellationToken);
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

	private async ValueTask<StunResponse?> RequestAsync(StunMessage5389 sendMessage, IPEndPoint remote, CancellationToken cancellationToken)
	{
		try
		{
			using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(ConnectTimeout);
			IDuplexPipe pipe = await _proxy.ConnectAsync(_lastLocalEndPoint, remote, cts.Token);
			try
			{
				int length = sendMessage.WriteTo(pipe.Output.GetSpan(sendMessage.Length));

				pipe.Output.Advance(length);
				await pipe.Output.FlushAsync(cancellationToken);

				StunMessage5389 message = new();
				bool success = await ReadPipeAsync(message, pipe.Input);

				if (success && message.IsSameTransaction(sendMessage))
				{
					IPEndPoint? local = _proxy.CurrentLocalEndPoint;
					if (local is not null)
					{
						_lastLocalEndPoint = local;
						return new StunResponse(message, remote, local);
					}
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

		return default;

		async ValueTask<bool> ReadPipeAsync(StunMessage5389 message, PipeReader reader)
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

	public void Dispose()
	{
		_proxy.Dispose();

		GC.SuppressFinalize(this);
	}
}
