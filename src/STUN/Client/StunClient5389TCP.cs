using STUN.Enums;
using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;

namespace STUN.Client;

public class StunClient5389TCP : IStunClient5389
{
	public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(3);

	private readonly IPEndPoint _remoteEndPoint;
	private IPEndPoint _lastLocalEndPoint;

	private readonly ITcpProxy _proxy;
	private readonly bool _ownedProxy;

	public StunResult5389 State { get; private set; } = new();

	public StunClient5389TCP(IPEndPoint server, IPEndPoint local, ITcpProxy? proxy = default, bool ownedProxy = true)
	{
		ArgumentNullException.ThrowIfNull(server);
		ArgumentNullException.ThrowIfNull(local);

		_proxy = proxy ?? new DirectTcpProxy();
		_ownedProxy = ownedProxy;

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

	public ValueTask FilteringBehaviorTestAsync(CancellationToken cancellationToken = default)
	{
		throw new NotSupportedException(@"Filtering test applies only to UDP.");
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
		if (_ownedProxy)
		{
			_proxy.Dispose();
		}

		GC.SuppressFinalize(this);
	}
}
