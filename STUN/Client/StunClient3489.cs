using Dns.Net.Abstractions;
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
		public IPEndPoint LocalEndPoint => Proxy.LocalEndPoint;

		public TimeSpan Timeout
		{
			get => Proxy.Timeout;
			set => Proxy.Timeout = value;
		}

		protected readonly IPAddress Server;
		protected readonly ushort Port;

		protected IPEndPoint RemoteEndPoint => new(Server, Port);

		protected readonly IUdpProxy Proxy;

		public ClassicStunResult Status { get; } = new();

		public StunClient3489(IDnsClient dnsQuery, string server, ushort port = 3478, IPEndPoint? local = null, IUdpProxy? proxy = null)
		{
			Proxy = proxy ?? new NoneUdpProxy(local);

			if (string.IsNullOrEmpty(server))
			{
				throw new ArgumentException(@"Please specify STUN server !");
			}

			if (port < 1)
			{
				throw new ArgumentException(@"Port value must be >= 1 !");
			}

			var ip = dnsQuery.Query(server);

			Server = ip;
			Port = port;

			Timeout = TimeSpan.FromSeconds(1.6);
			Status.LocalEndPoint = local;
		}

		private void Init()
		{
			Status.PublicEndPoint = default;
			Status.LocalEndPoint = default;
			Status.NatType = NatType.Unknown;
		}

		public async Task Query3489Async()
		{
			try
			{
				Init();
				using var cts = new CancellationTokenSource(Timeout);
				await Proxy.ConnectAsync(cts.Token);
				// test I
				var test1 = new StunMessage5389 { StunMessageType = StunMessageType.BindingRequest, MagicCookie = 0 };

				var (response1, remote1, local1) = await TestAsync(test1, RemoteEndPoint, RemoteEndPoint, cts.Token);
				if (response1 is null || remote1 is null)
				{
					Status.NatType = NatType.UdpBlocked;
					return;
				}

				Status.LocalEndPoint = local1 is null ? null : new IPEndPoint(local1, LocalEndPoint.Port);

				var mappedAddress1 = response1.GetMappedAddressAttribute();
				var changedAddress1 = response1.GetChangedAddressAttribute();

				// 某些单 IP 服务器的迷惑操作
				if (mappedAddress1 is null
				|| changedAddress1 is null
				|| Equals(changedAddress1.Address, remote1.Address)
				|| changedAddress1.Port == remote1.Port)
				{
					Status.NatType = NatType.UnsupportedServer;
					return;
				}

				Status.PublicEndPoint = mappedAddress1; // 显示 test I 得到的映射地址

				var test2 = new StunMessage5389
				{
					StunMessageType = StunMessageType.BindingRequest,
					MagicCookie = 0,
					Attributes = new[] { AttributeExtensions.BuildChangeRequest(true, true) }
				};

				// test II
				var (response2, remote2, _) = await TestAsync(test2, RemoteEndPoint, changedAddress1, cts.Token);
				var mappedAddress2 = response2.GetMappedAddressAttribute();

				if (Equals(mappedAddress1.Address, local1) && mappedAddress1.Port == LocalEndPoint.Port)
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
				if (response2 is not null && remote2 is not null)
				{
					// 有些单 IP 服务器并不能测 NAT 类型，比如 Google 的
					var type = Equals(remote1.Address, remote2.Address) || remote1.Port == remote2.Port ? NatType.UnsupportedServer : NatType.FullCone;
					Status.NatType = type;
					Status.PublicEndPoint = mappedAddress2;
					return;
				}

				// Test I(#2)
				var test12 = new StunMessage5389 { StunMessageType = StunMessageType.BindingRequest, MagicCookie = 0 };
				var (response12, _, _) = await TestAsync(test12, changedAddress1, changedAddress1, cts.Token);
				var mappedAddress12 = response12.GetMappedAddressAttribute();

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
				var test3 = new StunMessage5389
				{
					StunMessageType = StunMessageType.BindingRequest,
					MagicCookie = 0,
					Attributes = new[] { AttributeExtensions.BuildChangeRequest(false, true) }
				};
				var (response3, _, _) = await TestAsync(test3, changedAddress1, changedAddress1, cts.Token);
				var mappedAddress3 = response3.GetMappedAddressAttribute();
				if (mappedAddress3 is not null)
				{
					Status.NatType = NatType.RestrictedCone;
					Status.PublicEndPoint = mappedAddress3;
					return;
				}

				Status.NatType = NatType.PortRestrictedCone;
				Status.PublicEndPoint = mappedAddress12;
			}
			finally
			{
				await Proxy.DisconnectAsync();
			}
		}

		protected async Task<(StunMessage5389?, IPEndPoint?, IPAddress?)> TestAsync(StunMessage5389 sendMessage, IPEndPoint remote, IPEndPoint receive, CancellationToken token)
		{
			try
			{
				using var memoryOwner = MemoryPool<byte>.Shared.Rent(ushort.MaxValue);
				var sendBuffer = memoryOwner.Memory;
				var length = sendMessage.WriteTo(sendBuffer.Span);
				//var t = DateTime.Now;

				// Simple retransmissions
				//https://tools.ietf.org/html/rfc3489#section-9.3
				//while (t + TimeSpan.FromSeconds(3) > DateTime.Now)
				{
					try
					{
						var (receive1, ipe, local) = await Proxy.ReceiveAsync(sendBuffer[..length], remote, receive, token);

						var message = new StunMessage5389();
						if (message.TryParse(receive1) &&
							message.IsSameTransaction(sendMessage))
						{
							return (message, ipe, local);
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine(ex);
					}
				}
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
			return (null, null, null);
		}

		public virtual void Dispose()
		{
			Proxy.Dispose();
		}
	}
}
