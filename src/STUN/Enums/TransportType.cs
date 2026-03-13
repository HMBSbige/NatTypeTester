namespace STUN.Enums;

/// <summary>
/// Represents the transport protocol used for STUN communication.
/// </summary>
public enum TransportType
{
	/// <summary>
	/// User Datagram Protocol (UDP).
	/// </summary>
	Udp,

	/// <summary>
	/// Transmission Control Protocol (TCP).
	/// </summary>
	Tcp,

	/// <summary>
	/// Transport Layer Security (TLS) over TCP.
	/// </summary>
	Tls,

	/// <summary>
	/// Datagram Transport Layer Security (DTLS) over UDP.
	/// </summary>
	Dtls,
}
