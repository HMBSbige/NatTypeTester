using Dns.Net.Abstractions;
using STUN.Enums;
using STUN.Messages;
using STUN.Proxy;
using STUN.StunResult;
using STUN.Utils;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Client
{
	/// <summary>
	/// https://tools.ietf.org/html/rfc5389#section-7.2.1
	/// https://tools.ietf.org/html/rfc5780#section-4.2
	/// </summary>
	public class StunClient5389UDP : StunClient3489
	{
		public new StunResult5389 Status { get; } = new();

		public StunClient5389UDP(IDnsClient dnsQuery, string server, ushort port = 3478, IPEndPoint? local = null, IUdpProxy? proxy = null)
		: base(dnsQuery, server, port, local, proxy)
		{
			Timeout = TimeSpan.FromSeconds(3);
			Status.LocalEndPoint = local;
		}

		public async Task QueryAsync()
		{
			try
			{
				Status.Reset();
				using var cts = new CancellationTokenSource(Timeout);
				await Proxy.ConnectAsync(cts.Token);

				await FilteringBehaviorTestBaseAsync(cts.Token);
				if (Status.BindingTestResult != BindingTestResult.Success
					|| Status.FilteringBehavior == FilteringBehavior.UnsupportedServer
				)
				{
					return;
				}

				if (Equals(Status.PublicEndPoint, Status.LocalEndPoint))
				{
					Status.MappingBehavior = MappingBehavior.Direct;
					return;
				}

				// MappingBehaviorTest test II
				var (success2, result2) = await MappingBehaviorTestBase2Async(cts.Token);
				if (!success2)
				{
					return;
				}

				// MappingBehaviorTest test III
				await MappingBehaviorTestBase3Async(result2, cts.Token);
			}
			finally
			{
				await Proxy.DisconnectAsync();
			}
		}

		public async Task BindingTestAsync()
		{
			try
			{
				Status.Reset();
				using var cts = new CancellationTokenSource(Timeout);
				await Proxy.ConnectAsync(cts.Token);
				await BindingTestInternalAsync(cts.Token);
			}
			finally
			{
				await Proxy.DisconnectAsync();
			}
		}

		private async Task BindingTestInternalAsync(CancellationToken token)
		{
			Status.Clone(await BindingTestBaseAsync(RemoteEndPoint, token));
		}

		private async Task<StunResult5389> BindingTestBaseAsync(IPEndPoint remote, CancellationToken token)
		{
			var result = new StunResult5389();
			var test = new StunMessage5389 { StunMessageType = StunMessageType.BindingRequest };
			var (response1, _, local1) = await TestAsync(test, remote, remote, token);
			var mappedAddress1 = response1.GetXorMappedAddressAttribute();
			var otherAddress = response1.GetOtherAddressAttribute();
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
				Status.Reset();
				using var cts = new CancellationTokenSource(Timeout);
				await Proxy.ConnectAsync(cts.Token);

				// test I
				await BindingTestInternalAsync(cts.Token);
				if (Status.BindingTestResult != BindingTestResult.Success)
				{
					return;
				}

				if (Status.OtherEndPoint is null
					|| Equals(Status.OtherEndPoint.Address, RemoteEndPoint.Address)
					|| Status.OtherEndPoint.Port == RemoteEndPoint.Port)
				{
					Status.MappingBehavior = MappingBehavior.UnsupportedServer;
					return;
				}

				if (Equals(Status.PublicEndPoint, Status.LocalEndPoint))
				{
					Status.MappingBehavior = MappingBehavior.Direct;
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
				await Proxy.DisconnectAsync();
			}
		}

		private async Task<(bool, StunResult5389)> MappingBehaviorTestBase2Async(CancellationToken token)
		{
			var result2 = await BindingTestBaseAsync(new IPEndPoint(Status.OtherEndPoint!.Address, RemoteEndPoint.Port), token);
			if (result2.BindingTestResult != BindingTestResult.Success)
			{
				Status.MappingBehavior = MappingBehavior.Fail;
				return (false, result2);
			}

			if (Equals(result2.PublicEndPoint, Status.PublicEndPoint))
			{
				Status.MappingBehavior = MappingBehavior.EndpointIndependent;
				return (false, result2);
			}

			return (true, result2);
		}

		private async Task MappingBehaviorTestBase3Async(StunResult5389 result2, CancellationToken token)
		{
			var result3 = await BindingTestBaseAsync(Status.OtherEndPoint!, token);
			if (result3.BindingTestResult != BindingTestResult.Success)
			{
				Status.MappingBehavior = MappingBehavior.Fail;
				return;
			}

			Status.MappingBehavior = Equals(result3.PublicEndPoint, result2.PublicEndPoint) ? MappingBehavior.AddressDependent : MappingBehavior.AddressAndPortDependent;
		}

		private async Task FilteringBehaviorTestBaseAsync(CancellationToken token)
		{
			// test I
			await BindingTestInternalAsync(token);
			if (Status.BindingTestResult != BindingTestResult.Success)
			{
				return;
			}

			if (Status.OtherEndPoint is null
				|| Equals(Status.OtherEndPoint.Address, RemoteEndPoint.Address)
				|| Status.OtherEndPoint.Port == RemoteEndPoint.Port)
			{
				Status.FilteringBehavior = FilteringBehavior.UnsupportedServer;
				return;
			}

			// test II
			var test2 = new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingRequest,
				Attributes = new[] { AttributeExtensions.BuildChangeRequest(true, true) }
			};
			var (response2, _, _) = await TestAsync(test2, RemoteEndPoint, Status.OtherEndPoint, token);

			if (response2 is not null)
			{
				Status.FilteringBehavior = FilteringBehavior.EndpointIndependent;
				return;
			}

			// test III
			var test3 = new StunMessage5389
			{
				StunMessageType = StunMessageType.BindingRequest,
				Attributes = new[] { AttributeExtensions.BuildChangeRequest(false, true) }
			};
			var (response3, remote3, _) = await TestAsync(test3, RemoteEndPoint, RemoteEndPoint, token);

			if (response3 is null || remote3 is null)
			{
				Status.FilteringBehavior = FilteringBehavior.AddressAndPortDependent;
				return;
			}

			if (Equals(remote3.Address, RemoteEndPoint.Address) && remote3.Port != RemoteEndPoint.Port)
			{
				Status.FilteringBehavior = FilteringBehavior.AddressAndPortDependent;
			}
			else
			{
				Status.FilteringBehavior = FilteringBehavior.UnsupportedServer;
			}
		}

		public async Task FilteringBehaviorTestAsync()
		{
			try
			{
				Status.Reset();
				using var cts = new CancellationTokenSource(Timeout);
				await Proxy.ConnectAsync(cts.Token);
				await FilteringBehaviorTestBaseAsync(cts.Token);
			}
			finally
			{
				await Proxy.DisconnectAsync();
			}
		}
	}
}
