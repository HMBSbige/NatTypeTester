using System.Net;
using System.Net.Sockets;

namespace STUN.Proxy;

/// <summary>
/// A UDP proxy that communicates directly without any intermediary.
/// </summary>
public class NoneUdpProxy : IUdpProxy
{
	/// <summary>
	/// SIO_UDP_CONNRESET, i.e. _WSAIOW(IOC_VENDOR, 12)
	/// </summary>
	private const int SioUdpConnReset = -1744830452;

	/// <inheritdoc />
	public Socket Client { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="NoneUdpProxy"/> class bound to the specified local endpoint.
	/// </summary>
	/// <remarks>
	/// On Windows, the socket does not report ICMP port unreachable messages as <see cref="SocketError.ConnectionReset"/>,
	/// so that a closed port looks like a peer that does not respond, as it does on other platforms.
	/// </remarks>
	/// <param name="localEndPoint">The local endpoint to bind the UDP socket to.</param>
	public NoneUdpProxy(IPEndPoint localEndPoint)
	{
		Client = new Socket(localEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

		try
		{
			if (OperatingSystem.IsWindows())
			{
				Client.IOControl(SioUdpConnReset, [0, 0, 0, 0], null);
			}

			Client.Bind(localEndPoint);
		}
		catch
		{
			Client.Dispose();
			throw;
		}
	}

	/// <inheritdoc />
	public ValueTask ConnectAsync(CancellationToken cancellationToken = default)
	{
		return default;
	}

	/// <inheritdoc />
	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		return default;
	}

	/// <inheritdoc />
	public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
	{
		return Client.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
	{
		return Client.SendToAsync(buffer, socketFlags, remoteEP, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask DisposeAsync()
	{
		Dispose();
		GC.SuppressFinalize(this);
		return default;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		Client.Dispose();
		GC.SuppressFinalize(this);
	}
}
