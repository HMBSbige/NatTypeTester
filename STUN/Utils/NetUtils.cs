using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace STUN.Utils
{
	public static class NetUtils
	{
		public static TcpState GetState(this TcpClient tcpClient)
		{
			var foo = IPGlobalProperties
				.GetIPGlobalProperties()
				.GetActiveTcpConnections()
				.SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
			return foo?.State ?? TcpState.Unknown;
		}

		//TODO .NET6.0
		public static Task<(IPAddress, int, IPEndPoint)> ReceiveMessageFromAsync(this Socket client, EndPoint receive, byte[] array, SocketFlags flag)
		{
			return Task.Run(() =>
			{
				var length = client.ReceiveMessageFrom(array, 0, array.Length, ref flag, ref receive, out var ipPacketInformation);

				var local = ipPacketInformation.Address;

				Debug.WriteLine($@"{(IPEndPoint)receive} => {local} {length} 字节");
				return (local, length, (IPEndPoint)receive);
			});
		}

		//TODO Remove in .NET6.0
		public static ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(this Socket client, Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
		{
			client.ReceiveTimeout = (int)TimeSpan.FromSeconds(3).TotalMilliseconds;
			return new ValueTask<SocketReceiveMessageFromResult>(Task.Run(() =>
			{
				if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out var segment))
				{
					ThrowNotSupportedException();
				}

				var length = client.ReceiveMessageFrom(segment.Array!, segment.Offset, segment.Count, ref socketFlags, ref remoteEndPoint, out var ipPacketInformation);
				return new SocketReceiveMessageFromResult
				{
					ReceivedBytes = length,
					SocketFlags = socketFlags,
					RemoteEndPoint = remoteEndPoint,
					PacketInformation = ipPacketInformation
				};
			}, cancellationToken));

			static void ThrowNotSupportedException()
			{
				throw new NotSupportedException();
			}
		}

		//TODO Remove in .NET6.0
		public static async ValueTask<int> SendToAsync(this Socket client, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken)
		{
			byte[]? t = null;
			try
			{
				if (!MemoryMarshal.TryGetArray(buffer, out var segment))
				{
					t = ArrayPool<byte>.Shared.Rent(buffer.Length);
					buffer.CopyTo(t);
					segment = new ArraySegment<byte>(t, 0, buffer.Length);
				}

				return await client.SendToAsync(segment, socketFlags, remoteEP);
			}
			finally
			{
				if (t is not null)
				{
					ArrayPool<byte>.Shared.Return(t);
				}
			}
		}
	}
}
