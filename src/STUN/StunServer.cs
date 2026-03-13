using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;

namespace STUN;

/// <summary>
/// Represents a STUN server identified by a hostname and port.
/// </summary>
public class StunServer
{
	/// <summary>
	/// Gets the hostname or IP address of the STUN server.
	/// </summary>
	public string Hostname { get; }

	/// <summary>
	/// Gets the port number of the STUN server.
	/// </summary>
	public ushort Port { get; }

	/// <summary>
	/// The default STUN port (3478) as defined in RFC 5389.
	/// </summary>
	public const ushort DefaultPort = 3478;

	/// <summary>
	/// The default STUN over TLS port (5349) as defined in RFC 5389.
	/// </summary>
	public const ushort DefaultTlsPort = 5349;

	/// <summary>
	/// Initializes a new instance of the <see cref="StunServer"/> class with the default STUN server and port.
	/// </summary>
	public StunServer()
	{
		Hostname = @"stun.syncthing.net";
		Port = DefaultPort;
	}

	private StunServer(string hostname, ushort port)
	{
		Hostname = hostname;
		Port = port;
	}

	/// <summary>
	/// Attempts to parse a string into a <see cref="StunServer"/> instance.
	/// </summary>
	/// <param name="s">The string to parse, in the format "hostname" or "hostname:port".</param>
	/// <param name="result">When this method returns, contains the parsed <see cref="StunServer"/> if successful; otherwise, <see langword="null"/>.</param>
	/// <param name="defaultPort">The default port to use if none is specified in the string.</param>
	/// <returns><see langword="true"/> if the string was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public static bool TryParse(string s, [NotNullWhen(true)] out StunServer? result, ushort defaultPort = DefaultPort)
	{
		if (!HostnameEndpoint.TryParse(s, out HostnameEndpoint? host, defaultPort))
		{
			result = null;
			return false;
		}

		result = new StunServer(host.Hostname, host.Port);
		return true;
	}

	/// <summary>
	/// Returns the string representation of the STUN server in "hostname:port" format.
	/// The port is omitted when it equals <see cref="DefaultPort"/>.
	/// </summary>
	/// <returns>A string representation of the STUN server.</returns>
	public override string ToString()
	{
		if (Port is DefaultPort)
		{
			return Hostname;
		}

		if (IPAddress.TryParse(Hostname, out IPAddress? ip) && ip.AddressFamily is AddressFamily.InterNetworkV6)
		{
			return $@"[{ip}]:{Port}";
		}

		return $@"{Hostname}:{Port}";
	}
}
