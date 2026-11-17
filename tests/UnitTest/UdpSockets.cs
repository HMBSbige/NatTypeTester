using System.Net;
using System.Net.Sockets;

namespace UnitTest;

internal static class UdpSockets
{
	public static Socket Bind(IPAddress address, int port)
	{
		Socket socket = new(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

		try
		{
			socket.Bind(new IPEndPoint(address, port));
			return socket;
		}
		catch
		{
			socket.Dispose();
			throw;
		}
	}

	public static IPEndPoint GetLocalEndPoint(Socket socket)
	{
		return socket.LocalEndPoint as IPEndPoint ?? throw new InvalidOperationException(@"Socket is not bound.");
	}

	public static IPEndPoint GetClosedEndPoint()
	{
		using Socket socket = Bind(IPAddress.Loopback, 0);
		return GetLocalEndPoint(socket);
	}
}
