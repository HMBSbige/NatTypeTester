using Microsoft;
using Socks5.Clients;
using Socks5.Enums;
using Socks5.Models;
using Socks5.Utils;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace STUN.Proxy;

public class Socks5UdpProxy : IUdpProxy
{
	public Socket Client
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			Verify.Operation(_socks5Client?.UdpClient is not null, @"Socks5 is not established.");
			return _socks5Client.UdpClient;
		}
	}

	private readonly Socks5CreateOption _socks5Options;
	private readonly IPEndPoint _localEndPoint;

	private Socks5Client? _socks5Client;
	private ServerBound _udpServerBound;

	public Socks5UdpProxy(IPEndPoint localEndPoint, Socks5CreateOption socks5Options)
	{
		Requires.NotNull(localEndPoint, nameof(localEndPoint));
		Requires.NotNull(socks5Options, nameof(socks5Options));
		Requires.Argument(socks5Options.Address is not null, nameof(socks5Options), @"SOCKS5 address is null");

		_localEndPoint = localEndPoint;
		_socks5Options = socks5Options;
	}

	public async ValueTask ConnectAsync(CancellationToken cancellationToken = default)
	{
		Verify.Operation(_socks5Client?.Status is not Status.Established, @"SOCKS5 client has been connected");
		_socks5Client?.Dispose();

		_socks5Client = new Socks5Client(_socks5Options);
		_udpServerBound = await _socks5Client.UdpAssociateAsync(_localEndPoint.Address, (ushort)_localEndPoint.Port, cancellationToken);
	}

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		if (_socks5Client is not null)
		{
			_socks5Client.Dispose();
			_socks5Client = null;
		}
		return default;
	}

	public async ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default)
	{
		Verify.Operation(_socks5Client?.Status is Status.Established && _socks5Client.UdpClient is not null, @"Socks5 is not established.");

		byte[] t = ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			if (_udpServerBound.Type is AddressType.Domain)
			{
				ThrowErrorAddressType();
			}

			IPEndPoint remote = new(_udpServerBound.Address!, _udpServerBound.Port);
			SocketReceiveMessageFromResult r = await _socks5Client.UdpClient.ReceiveMessageFromAsync(t, socketFlags, remote, cancellationToken);
			Socks5UdpReceivePacket u = Unpack.Udp(t.AsMemory(0, r.ReceivedBytes));

			u.Data.CopyTo(buffer);

			if (u.Type is AddressType.Domain)
			{
				ThrowErrorAddressType();
			}

			return new SocketReceiveMessageFromResult
			{
				ReceivedBytes = u.Data.Length,
				SocketFlags = r.SocketFlags,
				RemoteEndPoint = new IPEndPoint(u.Address!, u.Port),
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

	public async ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default)
	{
		Verify.Operation(_socks5Client is not null, @"SOCKS5 client is not connected");

		if (remoteEP is not IPEndPoint remote)
		{
			ThrowNotSupportedException();
		}

		return await _socks5Client.SendUdpAsync(buffer, remote.Address, (ushort)remote.Port, cancellationToken);

		static void ThrowNotSupportedException()
		{
			throw new NotSupportedException();
		}
	}

	public void Dispose()
	{
		_socks5Client?.Dispose();
		GC.SuppressFinalize(this);
	}
}
