using Socks5.Clients;
using Socks5.Enums;
using Socks5.Models;
using Socks5.Utils;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace STUN.Proxy;

/// <summary>
/// A UDP proxy that routes datagrams through a SOCKS5 proxy server using UDP association.
/// </summary>
public class Socks5UdpProxy : IUdpProxy
{
	/// <inheritdoc />
	public Socket Client
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Socks5Client? socks5Client = _socks5Client;
			if (socks5Client?.UdpClient is null)
			{
				throw new InvalidOperationException(@"Socks5 is not established.");
			}

			return socks5Client.UdpClient;
		}
	}

	private readonly Socks5CreateOption _socks5Options;
	private readonly IPEndPoint _localEndPoint;

	private Socks5Client? _socks5Client;
	private ServerBound _udpServerBound;

	/// <summary>
	/// Initializes a new instance of the <see cref="Socks5UdpProxy"/> class with the specified local endpoint and SOCKS5 options.
	/// </summary>
	/// <param name="localEndPoint">The local endpoint to bind the UDP socket to.</param>
	/// <param name="socks5Options">The SOCKS5 connection options.</param>
	public Socks5UdpProxy(IPEndPoint localEndPoint, Socks5CreateOption socks5Options)
	{
		ArgumentNullException.ThrowIfNull(localEndPoint);
		ArgumentNullException.ThrowIfNull(socks5Options);
		ArgumentNullException.ThrowIfNull(socks5Options.Address, nameof(socks5Options.Address));

		_localEndPoint = localEndPoint;
		_socks5Options = socks5Options;
	}

	/// <inheritdoc />
	public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
	{
		if (_socks5Client?.Status is Status.Established)
		{
			throw new InvalidOperationException(@"SOCKS5 client has been connected");
		}

		_socks5Client?.Dispose();

		_socks5Client = new Socks5Client(_socks5Options);
		_udpServerBound = await _socks5Client.UdpAssociateAsync(_localEndPoint.Address, (ushort)_localEndPoint.Port, cancellationToken);
	}

	/// <inheritdoc />
	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		_socks5Client?.Dispose();
		_socks5Client = null;
		return default;
	}

	/// <inheritdoc />
	public async ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
	{
		Socks5Client? socks5Client = _socks5Client;
		Socket? udpClient = socks5Client?.UdpClient;
		if (socks5Client?.Status is not Status.Established || udpClient is null)
		{
			throw new InvalidOperationException(@"Socks5 is not established.");
		}

		byte[] t = ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			if (_udpServerBound.Type is AddressType.Domain || _udpServerBound.Address is not { } udpServerAddress)
			{
				ThrowErrorAddressType();
			}

			IPEndPoint remote = new(udpServerAddress, _udpServerBound.Port);
			SocketReceiveMessageFromResult r = await udpClient.ReceiveMessageFromAsync(t, socketFlags, remote, cancellationToken);
			Socks5UdpReceivePacket u = Unpack.Udp(t.AsMemory(0, r.ReceivedBytes));

			u.Data.CopyTo(buffer);

			if (u.Type is AddressType.Domain || u.Address is not { } remoteAddress)
			{
				ThrowErrorAddressType();
			}

			return new SocketReceiveMessageFromResult
			{
				ReceivedBytes = u.Data.Length,
				SocketFlags = r.SocketFlags,
				RemoteEndPoint = new IPEndPoint(remoteAddress, u.Port),
				PacketInformation = r.PacketInformation
			};
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(t);
		}

		static void ThrowErrorAddressType()
		{
			throw new InvalidDataException(@"Received error AddressType");
		}
	}

	/// <inheritdoc />
	public async ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
	{
		Socks5Client socks5Client = _socks5Client ?? throw new InvalidOperationException(@"SOCKS5 client is not connected");

		if (remoteEP is not IPEndPoint remote)
		{
			ThrowNotSupportedException();
		}

		return await socks5Client.SendUdpAsync(buffer, remote.Address, (ushort)remote.Port, cancellationToken);

		static void ThrowNotSupportedException()
		{
			throw new NotSupportedException();
		}
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
		_socks5Client?.Dispose();
		GC.SuppressFinalize(this);
	}
}
