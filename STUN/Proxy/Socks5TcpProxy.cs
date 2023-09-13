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

	protected readonly Socks5CreateOption Socks5Options;

	protected Socks5Client? Socks5Client;

	public Socks5TcpProxy(Socks5CreateOption socks5Options)
	{
		Requires.NotNull(socks5Options, nameof(socks5Options));
		Requires.Argument(socks5Options.Address is not null, nameof(socks5Options), @"SOCKS5 address is null");

		Socks5Options = socks5Options;
	}

	public virtual async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);
		Requires.NotNull(local, nameof(local));
		Requires.NotNull(dst, nameof(dst));

		await CloseAsync(cancellationToken);

		Socks5Client = new Socks5Client(Socks5Options);

		GetTcpClient()?.Client.Bind(local);

		await Socks5Client.ConnectAsync(dst.Address, (ushort)dst.Port, cancellationToken);

		return Socks5Client.GetPipe();
	}

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		Verify.NotDisposed(this);

		CloseClient();

		return default;
	}

	protected TcpClient? GetTcpClient()
	{
		// TODO
		return Socks5Client?.GetType().GetField(@"_tcpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(Socks5Client) as TcpClient;
	}

	protected virtual void CloseClient()
	{
		if (Socks5Client is null)
		{
			return;
		}

		try
		{
			GetTcpClient()?.Client.Close(0);
		}
		finally
		{
			Socks5Client.Dispose();
			Socks5Client = default;
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
