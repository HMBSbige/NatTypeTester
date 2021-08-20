using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
	}
}
