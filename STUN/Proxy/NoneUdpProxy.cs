using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Proxy
{
	public class NoneUdpProxy : IUdpProxy
	{
		public TimeSpan Timeout
		{
			get => TimeSpan.FromMilliseconds(_udpClient.Client.ReceiveTimeout);
			set => _udpClient.Client.ReceiveTimeout = Convert.ToInt32(value.TotalMilliseconds);
		}

		public IPEndPoint LocalEndPoint => (IPEndPoint)_udpClient.Client.LocalEndPoint;

		private readonly UdpClient _udpClient;

		public NoneUdpProxy(IPEndPoint? local)
		{
			_udpClient = local is null ? new UdpClient() : new UdpClient(local);
		}

		public Task ConnectAsync(CancellationToken token = default)
		{
			return Task.CompletedTask;
		}

		public Task DisconnectAsync()
		{
			_udpClient.Close();
			return Task.CompletedTask;
		}

		public async Task<(byte[], IPEndPoint, IPAddress)> ReceiveAsync(byte[] bytes, IPEndPoint remote, EndPoint receive, CancellationToken token = default)
		{
			var localEndPoint = (IPEndPoint)_udpClient.Client.LocalEndPoint;

			Debug.WriteLine($@"{localEndPoint} => {remote} {bytes.Length} 字节");

			await _udpClient.SendAsync(bytes, bytes.Length, remote);

			var res = new byte[ushort.MaxValue];
			var flag = SocketFlags.None;

			var length = _udpClient.Client.ReceiveMessageFrom(res, 0, res.Length, ref flag, ref receive, out var ipPacketInformation);

			var local = ipPacketInformation.Address;

			Debug.WriteLine($@"{(IPEndPoint)receive} => {local} {length} 字节");

			return (res.Take(length).ToArray(),
					(IPEndPoint)receive
					, local);
		}

		public void Dispose()
		{
			_udpClient.Dispose();
		}
	}
}
