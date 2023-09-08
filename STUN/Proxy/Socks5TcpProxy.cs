using Microsoft;
using Socks5.Clients;
using Socks5.Models;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace STUN.Proxy;

public class Socks5TcpProxy : ITcpProxy, IDisposableObservable
{
	public IPEndPoint? CurrentLocalEndPoint
	{
		get
		{
			Verify.NotDisposed(this);
			return GetTcpClient()?.Client.LocalEndPoint as IPEndPoint;
		}
	}

	private readonly Socks5CreateOption _socks5Options;

	private Socks5Client? _socks5Client;

	public Socks5TcpProxy(Socks5CreateOption socks5Options)
	{
		Requires.NotNull(socks5Options, nameof(socks5Options));
		Requires.Argument(socks5Options.Address is not null, nameof(socks5Options), @"SOCKS5 address is null");

		_socks5Options = socks5Options;
	}

	public async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);
		Requires.NotNull(local, nameof(local));
		Requires.NotNull(dst, nameof(dst));

		await CloseAsync(cancellationToken);

		_socks5Client = new Socks5Client(_socks5Options);

		GetTcpClient()?.Client.Bind(local);

		await _socks5Client.ConnectAsync(dst.Address, (ushort)dst.Port, cancellationToken);

		return _socks5Client.GetPipe();
	}

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);

		CloseClient();

		return default;
	}

	private TcpClient? GetTcpClient()
	{
		// TODO
		return _socks5Client?.GetType().GetField(@"_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(_socks5Client) as TcpClient;
	}

	private void CloseClient()
	{
		if (_socks5Client is null)
		{
			return;
		}

		try
		{
			GetTcpClient()?.Client.Close(0);
		}
		finally
		{
			_socks5Client.Dispose();
			_socks5Client = default;
		}
	}

	public bool IsDisposed { get; private set; }

	public void Dispose()
	{
		IsDisposed = true;

		CloseClient();

		GC.SuppressFinalize(this);
	}
}
