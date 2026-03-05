using Socks5.Clients;
using Socks5.Models;
using System.IO.Pipelines;
using System.Net;

namespace STUN.Proxy;

public class Socks5TcpProxy : ITcpProxy
{
	public IPEndPoint? CurrentLocalEndPoint
	{
		get
		{
			ObjectDisposedException.ThrowIf(IsDisposed, this);
			return Socks5Client?.TcpClient.Client.LocalEndPoint as IPEndPoint;
		}
	}

	protected readonly Socks5CreateOption Socks5Options;

	protected Socks5Client? Socks5Client;

	public Socks5TcpProxy(Socks5CreateOption socks5Options)
	{
		ArgumentNullException.ThrowIfNull(socks5Options);
		ArgumentNullException.ThrowIfNull(socks5Options.Address, nameof(socks5Options.Address));

		Socks5Options = socks5Options;
	}

	public virtual async ValueTask<IDuplexPipe> ConnectAsync(IPEndPoint local, IPEndPoint dst, CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);
		ArgumentNullException.ThrowIfNull(local);
		ArgumentNullException.ThrowIfNull(dst);

		await CloseAsync(cancellationToken);

		Socks5Client = new Socks5Client(Socks5Options);

		Socks5Client.TcpClient.Client.Bind(local);

		await Socks5Client.ConnectAsync(dst.Address, (ushort)dst.Port, cancellationToken);

		return Socks5Client.GetPipe();
	}

	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		CloseClient();

		return default;
	}

	protected virtual void CloseClient()
	{
		if (Socks5Client is null)
		{
			return;
		}

		try
		{
			Socks5Client.TcpClient.Client.Close(0);
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
