using Microsoft;
using STUN.Enums;
using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using STUN.Utils;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Client
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5389#section-7.2.1
	/// https://tools.ietf.org/html/rfc5780#section-4.2
	/// </summary>
	public class StunClient5389UDP : IStunClient
	{
		public virtual IPEndPoint LocalEndPoint => (IPEndPoint)_proxy.Client.LocalEndPoint!;

		public TimeSpan ReceiveTimeout { get; set; } = TimeSpan.FromSeconds(3);

		private readonly IPEndPoint _remoteEndPoint;

		private readonly IUdpProxy _proxy;

		public StunResult5389 State { get; } = new();

		public StunClient5389UDP(IPAddress server, ushort port, IPEndPoint local, IUdpProxy? proxy = null)
		{
			Requires.NotNull(server, nameof(server));
			Requires.Argument(port > 0, nameof(port), @"Port value must be > 0!");

			_proxy = proxy ?? new NoneUdpProxy(local);

			_remoteEndPoint = new IPEndPoint(server, port);

			State.LocalEndPoint = local;
		}

		public virtual async ValueTask ConnectProxyAsync(CancellationToken cancellationToken = default)
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(ReceiveTimeout);

			await _proxy.ConnectAsync(cts.Token);
		}

		public virtual async ValueTask CloseProxyAsync(CancellationToken cancellationToken = default)
		{
			await _proxy.CloseAsync(cancellationToken);
		}

		public async ValueTask QueryAsync(CancellationToken cancellationToken = default)
		{
			State.Reset();

			await FilteringBehaviorTestBaseAsync(cancellationToken);
			if (State.BindingTestResult != BindingTestResult.Success
				|| State.FilteringBehavior == FilteringBehavior.UnsupportedServer
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
			var (success2, result2) = await MappingBehaviorTestBase2Async(cancellationToken);
			if (!success2)
			{
				return;
			}

			// MappingBehaviorTest test III
			await MappingBehaviorTestBase3Async(result2, cancellationToken);
		}

		public async Task BindingTestAsync()
		{
			try
			{
				State.Reset();
				using var cts = new CancellationTokenSource(ReceiveTimeout);
				await _proxy.ConnectAsync(cts.Token);
				await BindingTestInternalAsync(cts.Token);
			}
			finally
			{
				await _proxy.ConnectAsync();
			}
		}

		private async Task BindingTestInternalAsync(CancellationToken token)
		{
			State.Clone(await BindingTestBaseAsync(_remoteEndPoint, token));
		}

		private async Task<StunResult5389> BindingTestBaseAsync(IPEndPoint remote, CancellationToken token)
		{
			var result = new StunResult5389();
			var test = new StunMessage5389 { StunMessageType = StunMessageType.BindingRequest };
			var (response1, _, local1) = await TestAsync(test, remote, remote, token);
			var mappedAddress1 = response1?.GetXorMappedAddressAttribute();
			var otherAddress = response1?.GetOtherAddressAttribute();
			var local = local1 is null ? null : new IPEndPoint(local1, LocalEndPoint.Port);

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

			result.LocalEndPoint = local;
			result.PublicEndPoint = mappedAddress1;
			result.OtherEndPoint = otherAddress;

			return result;
		}

		public async Task MappingBehaviorTestAsync()
		{
			try
			{
				State.Reset();
				using var cts = new CancellationTokenSource(ReceiveTimeout);
				await _proxy.ConnectAsync(cts.Token);

				// test I
				await BindingTestInternalAsync(cts.Token);
				if (State.BindingTestResult != BindingTestResult.Success)
				{
					return;
				}

				if (State.OtherEndPoint is null
					|| Equals(State.OtherEndPoint.Address, _remoteEndPoint.Address)
					|| State.OtherEndPoint.Port == _remoteEndPoint.Port)
				{
					State.MappingBehavior = MappingBehavior.UnsupportedServer;
					return;
				}

				if (Equals(State.PublicEndPoint, State.LocalEndPoint))
				{
					State.MappingBehavior = MappingBehavior.Direct;
					return;
				}

				// test II
				var (success2, result2) = await MappingBehaviorTestBase2Async(cts.Token);
				if (!success2)
				{
					return;
				}

				// test III
				await MappingBehaviorTestBase3Async(result2, cts.Token);
			}
			finally
			{
				await _proxy.CloseAsync();
			}
		}

		private async Task<(bool, StunResult5389)> MappingBehaviorTestBase2Async(CancellationToken token)
		{
			var result2 = await BindingTestBaseAsync(new IPEndPoint(State.OtherEndPoint!.Address, _remoteEndPoint.Port), token);
			if (result2.BindingTestResult != BindingTestResult.Success)
			{
				State.MappingBehavior = MappingBehavior.Fail;
				return (false, result2);
			}

			if (Equals(result2.PublicEndPoint, State.PublicEndPoint))
			{
				State.MappingBehavior = MappingBehavior.EndpointIndependent;
				return (false, result2);
			}

			return (true, result2);
		}

		private async Task MappingBehaviorTestBase3Async(StunResult5389 result2, CancellationToken token)
		{
			var result3 = await BindingTestBaseAsync(State.OtherEndPoint!, token);
			if (result3.BindingTestResult != BindingTestResult.Success)
			{
				State.MappingBehavior = MappingBehavior.Fail;
				return;
			}

			State.MappingBehavior = Equals(result3.PublicEndPoint, result2.PublicEndPoint) ? MappingBehavior.AddressDependent : MappingBehavior.AddressAndPortDependent;
		}

		private async Task FilteringBehaviorTestBaseAsync(CancellationToken token)
		{
			// test I
			await BindingTestInternalAsync(token);
			if (State.BindingTestResult != BindingTestResult.Success)
			{
				return;
			}

			if (State.OtherEndPoint is null
				|| Equals(State.OtherEndPoint.Address, _remoteEndPoint.Address)
				|| State.OtherEndPoint.Port == _remoteEndPoint.Port)
			{
				State.FilteringBehavior = FilteringBehavior.UnsupportedServer;
				return;
			}

			// test II
			var test2 = new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingRequest,
				Attributes = new[] { AttributeExtensions.BuildChangeRequest(true, true) }
			};
			var (response2, _, _) = await TestAsync(test2, _remoteEndPoint, State.OtherEndPoint, token);

			if (response2 is not null)
			{
				State.FilteringBehavior = FilteringBehavior.EndpointIndependent;
				return;
			}

			// test III
			var test3 = new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingRequest,
				Attributes = new[] { AttributeExtensions.BuildChangeRequest(false, true) }
			};
			var (response3, remote3, _) = await TestAsync(test3, _remoteEndPoint, _remoteEndPoint, token);

			if (response3 is null || remote3 is null)
			{
				State.FilteringBehavior = FilteringBehavior.AddressAndPortDependent;
				return;
			}

			if (Equals(remote3.Address, _remoteEndPoint.Address) && remote3.Port != _remoteEndPoint.Port)
			{
				State.FilteringBehavior = FilteringBehavior.AddressAndPortDependent;
			}
			else
			{
				State.FilteringBehavior = FilteringBehavior.UnsupportedServer;
			}
		}

		public async Task FilteringBehaviorTestAsync()
		{
			try
			{
				State.Reset();
				using var cts = new CancellationTokenSource(ReceiveTimeout);
				await _proxy.ConnectAsync(cts.Token);
				await FilteringBehaviorTestBaseAsync(cts.Token);
			}
			finally
			{
				await _proxy.CloseAsync();
			}
		}

		private async Task<(StunMessage5389?, IPEndPoint?, IPAddress?)> TestAsync(StunMessage5389 sendMessage, IPEndPoint remote, IPEndPoint receive, CancellationToken cancellationToken)
		{
			try
			{
				using var memoryOwner = MemoryPool<byte>.Shared.Rent(0x10000);
				var buffer = memoryOwner.Memory;
				var length = sendMessage.WriteTo(buffer.Span);

				await _proxy.SendToAsync(buffer[..length], SocketFlags.None, remote, cancellationToken);

				using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				cts.CancelAfter(ReceiveTimeout);
				var r = await _proxy.ReceiveMessageFromAsync(buffer, SocketFlags.None, receive, cts.Token);

				var message = new StunMessage5389();
				if (message.TryParse(buffer.Span[..r.ReceivedBytes]) && message.IsSameTransaction(sendMessage))
				{
					return (message, (IPEndPoint)r.RemoteEndPoint, r.PacketInformation.Address);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}

			return (null, null, null);
		}

		public void Dispose()
		{
			_proxy.Dispose();
		}
	}
}
