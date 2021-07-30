using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace STUN.Utils
{
	public static class NetUtils
	{
		private static Regex HostnameRegex{get;}=new Regex(@"^[a-zA-Z0-9\.\-]+$",RegexOptions.Compiled);
		private static Regex IpV4Regex{get;}=new Regex(@"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$",RegexOptions.Compiled);
		private static Regex IpV6Regex{get;}=new Regex(@"^\[[a-fA-F0-9\:]+\]$",RegexOptions.Compiled);

		public static IPEndPoint? ParseEndpoint(string? str){
			if (string.IsNullOrWhiteSpace(str)){return null;}

#pragma warning disable CS8602 // 解引用可能出现空引用。
			//此处分析器异常，上面已做空值判断
			var splitIndex=str.LastIndexOf(":");
#pragma warning restore CS8602 // 解引用可能出现空引用。

			var addressString=str.Substring(0,splitIndex);
			var portString=str.Substring(splitIndex+1);
			if(!ushort.TryParse(portString,out var port) || port<ushort.MinValue || port>ushort.MaxValue){return null;}
			
			if(HostnameRegex.IsMatch(addressString)){
				IPAddress[] addressArray;
				try{
					//此处使用异步较好
					addressArray=Dns.GetHostAddresses(addressString);
				}catch{
					return null;
				}
				if(addressArray.GetLength(0)<1){return null;}
				try{
					return new IPEndPoint(addressArray[0],port);
				}catch{
					return null;
				}
				
			}

			if (IpV6Regex.IsMatch(addressString) || IpV4Regex.IsMatch(addressString)){
				if(!IPAddress.TryParse(addressString,out IPAddress address)){return null;}
				try{
					return new IPEndPoint(address,port);
				}catch{
					return null;
				}
			}

			return null;
		}

		public static TcpState GetState(this TcpClient tcpClient)
		{
			var foo = IPGlobalProperties
				.GetIPGlobalProperties()
				.GetActiveTcpConnections()
				.SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));
			return foo?.State ?? TcpState.Unknown;
		}

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
