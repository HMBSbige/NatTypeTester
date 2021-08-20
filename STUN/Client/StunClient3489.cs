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
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Client
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc3489#section-10.1
	/// https://upload.wikimedia.org/wikipedia/commons/6/63/STUN_Algorithm3.svg
	/// </summary>
	public class StunClient3489 : IDisposable
	{
		public virtual IPEndPoint LocalEndPoint => _proxy.LocalEndPoint;

		public TimeSpan Timeout
		{
			get => _proxy.Timeout;
			set => _proxy.Timeout = value;
		}

		private readonly IPEndPoint _remoteEndPoint;

		private readonly IUdpProxy _proxy;

		public ClassicStunResult Status { get; } = new();

		public StunClient3489(IPAddress server, ushort port = 3478, IPEndPoint? local = null, IUdpProxy? proxy = null)
		{
			Requires.NotNull(server, nameof(server));
			Requires.Argument(port > 0, nameof(port), @"Port value must be > 0!");

			_proxy = proxy ?? new NoneUdpProxy(local);

			_remoteEndPoint = new IPEndPoint(server, port);

			Timeout = TimeSpan.FromSeconds(3);
			Status.LocalEndPoint = local;
		}

		public virtual async ValueTask ConnectAsync(CancellationToken cancellationToken)
		{
			Status.Reset();

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cts.CancelAfter(Timeout);

			await _proxy.ConnectAsync(cts.Token);
		}

		public virtual async ValueTask DisconnectAsync()
		{
			await _proxy.DisconnectAsync();
		}

		public async Task QueryAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				await ConnectAsync(cancellationToken);

				// test I
				var response1 = await Test1Async(cancellationToken);
				if (response1 is null)
				{
					Status.NatType = NatType.UdpBlocked;
					return;
				}

				Status.LocalEndPoint = new IPEndPoint(response1.LocalAddress, LocalEndPoint.Port);

				var mappedAddress1 = response1.Message.GetMappedAddressAttribute();
				var changedAddress = response1.Message.GetChangedAddressAttribute();

				Status.PublicEndPoint = mappedAddress1; // 显示 test I 得到的映射地址

				// 某些单 IP 服务器的迷惑操作
				if (mappedAddress1 is null || changedAddress is null
					|| Equals(changedAddress.Address, response1.Remote.Address)
					|| changedAddress.Port == response1.Remote.Port)
				{
					Status.NatType = NatType.UnsupportedServer;
					return;
				}

				// test II
				var response2 = await Test2Async(changedAddress, cancellationToken);
				var mappedAddress2 = response2?.Message.GetMappedAddressAttribute();

				// is Public IP == link's IP?
				if (Equals(mappedAddress1.Address, response1.LocalAddress) && mappedAddress1.Port == LocalEndPoint.Port)
				{
					// No NAT
					if (response2 is null)
					{
						Status.NatType = NatType.SymmetricUdpFirewall;
						Status.PublicEndPoint = mappedAddress1;
					}
					else
					{
						Status.NatType = NatType.OpenInternet;
						Status.PublicEndPoint = mappedAddress2;
					}
					return;
				}

				// NAT
				if (response2 is not null)
				{
					// 有些单 IP 服务器并不能测 NAT 类型，比如 Google 的
					var type = Equals(response1.Remote.Address, response2.Remote.Address) || response1.Remote.Port == response2.Remote.Port ? NatType.UnsupportedServer : NatType.FullCone;
					Status.NatType = type;
					Status.PublicEndPoint = mappedAddress2;
					return;
				}

				// Test I(#2)
				var response12 = await Test1_2Async(changedAddress, cancellationToken);
				var mappedAddress12 = response12?.Message.GetMappedAddressAttribute();

				if (mappedAddress12 is null)
				{
					Status.NatType = NatType.Unknown;
					return;
				}

				if (!Equals(mappedAddress12, mappedAddress1))
				{
					Status.NatType = NatType.Symmetric;
					Status.PublicEndPoint = mappedAddress12;
					return;
				}

				// Test III
				var response3 = await Test3Async(cancellationToken);
				if (response3 is not null)
				{
					var mappedAddress3 = response3.Message.GetMappedAddressAttribute();
					if (mappedAddress3 is not null
						&& Equals(response3.Remote.Address, response1.Remote.Address)
						&& response3.Remote.Port != response1.Remote.Port)
					{
						Status.NatType = NatType.RestrictedCone;
						Status.PublicEndPoint = mappedAddress3;
						return;
					}
				}

				Status.NatType = NatType.PortRestrictedCone;
				Status.PublicEndPoint = mappedAddress12;
			}
			finally
			{
				await DisconnectAsync();
			}
		}

		private async ValueTask<StunResponse?> RequestAsync(StunMessage5389 sendMessage, IPEndPoint remote, IPEndPoint receive, CancellationToken cancellationToken)
		{
			try
			{
				using var memoryOwner = MemoryPool<byte>.Shared.Rent(ushort.MaxValue);
				var sendBuffer = memoryOwner.Memory;
				var length = sendMessage.WriteTo(sendBuffer.Span);

				using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				cts.CancelAfter(Timeout);
				var (receiveBuffer, ipe, local) = await _proxy.ReceiveAsync(sendBuffer[..length], remote, receive, cts.Token);

				var message = new StunMessage5389();
				if (message.TryParse(receiveBuffer) && message.IsSameTransaction(sendMessage))
				{
					return new StunResponse(message, ipe, local);
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			return default;
		}

		public virtual async ValueTask<StunResponse?> Test1Async(CancellationToken cancellationToken)
		{
			var message = new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingRequest,
				MagicCookie = 0
			};
			return await RequestAsync(message, _remoteEndPoint, _remoteEndPoint, cancellationToken);
		}

		public virtual async ValueTask<StunResponse?> Test2Async(IPEndPoint other, CancellationToken cancellationToken)
		{
			var message = new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingRequest,
				MagicCookie = 0,
				Attributes = new[] { AttributeExtensions.BuildChangeRequest(true, true) }
			};
			return await RequestAsync(message, _remoteEndPoint, other, cancellationToken);
		}

		public virtual async ValueTask<StunResponse?> Test1_2Async(IPEndPoint other, CancellationToken cancellationToken)
		{
			var message = new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingRequest,
				MagicCookie = 0
			};
			return await RequestAsync(message, other, other, cancellationToken);
		}

		public virtual async ValueTask<StunResponse?> Test3Async(CancellationToken cancellationToken)
		{
			var message = new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingRequest,
				MagicCookie = 0,
				Attributes = new[] { AttributeExtensions.BuildChangeRequest(false, true) }
			};
			return await RequestAsync(message, _remoteEndPoint, _remoteEndPoint, cancellationToken);
		}

		public void Dispose()
		{
			_proxy.Dispose();
		}
	}
}
