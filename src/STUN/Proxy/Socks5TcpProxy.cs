using Socks5.Clients;
using Socks5.Models;
using System.IO.Pipelines;
using System.Net;

namespace STUN.Proxy;

/// <summary>
/// A TCP proxy that routes connections through a SOCKS5 proxy server.
/// </summary>
public class Socks5TcpProxy : ITcpProxy
{
	/// <inheritdoc />
	public IPEndPoint? CurrentLocalEndPoint
	{
		get
		{
			ObjectDisposedException.ThrowIf(IsDisposed, this);
			return Socks5Client?.TcpClient.Client.LocalEndPoint as IPEndPoint;
		}
	}

	/// <summary>
	/// The SOCKS5 connection options.
	/// </summary>
	protected readonly Socks5CreateOption Socks5Options;

	/// <summary>
	/// The underlying SOCKS5 client used for proxied connections.
	/// </summary>
	protected Socks5Client? Socks5Client;

	/// <summary>
	/// Initializes a new instance of the <see cref="Socks5TcpProxy"/> class with the specified SOCKS5 options.
	/// </summary>
	/// <param name="socks5Options">The SOCKS5 connection options.</param>
	public Socks5TcpProxy(Socks5CreateOption socks5Options)
	{
		ArgumentNullException.ThrowIfNull(socks5Options);
		ArgumentNullException.ThrowIfNull(socks5Options.Address, nameof(socks5Options.Address));

		Socks5Options = socks5Options;
	}

	/// <inheritdoc />
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

	/// <inheritdoc />
	public ValueTask CloseAsync(CancellationToken cancellationToken = default)
	{
		ObjectDisposedException.ThrowIf(IsDisposed, this);

		CloseClient();

		return default;
	}

	/// <summary>
	/// Closes the underlying SOCKS5 client and releases its resources.
	/// </summary>
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

	/// <summary>
	/// Gets a value indicating whether this proxy has been disposed.
	/// </summary>
	public bool IsDisposed { get; private set; }

	/// <inheritdoc />
	public void Dispose()
	{
		IsDisposed = true;

		CloseClient();

		GC.SuppressFinalize(this);
	}
}
