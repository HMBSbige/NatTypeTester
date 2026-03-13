namespace STUN.Enums;

/// <summary>
/// STUN Attribute Registry
/// </summary>
/// <remarks>
/// https://datatracker.ietf.org/doc/html/rfc3489#section-11.2
/// https://datatracker.ietf.org/doc/html/rfc5389#section-18.2
/// https://datatracker.ietf.org/doc/html/rfc5780#section-9.1
/// https://datatracker.ietf.org/doc/html/rfc8489#section-18.3
/// </remarks>
public enum AttributeType : ushort
{
	/// <summary>
	/// Placeholder for unrecognized or unused attribute types.
	/// </summary>
	Useless = 0x0000,

	/// <summary>
	/// MAPPED-ADDRESS attribute containing the mapped transport address (RFC 3489).
	/// </summary>
	MappedAddress = 0x0001,

	/// <summary>
	/// RESPONSE-ADDRESS attribute indicating where to send the response (RFC 3489, deprecated).
	/// </summary>
	ResponseAddress = 0x0002,

	/// <summary>
	/// CHANGE-REQUEST attribute used to request a change of IP and/or port (RFC 3489 / RFC 5780).
	/// </summary>
	ChangeRequest = 0x0003,

	/// <summary>
	/// SOURCE-ADDRESS attribute containing the source transport address (RFC 3489, deprecated).
	/// </summary>
	SourceAddress = 0x0004,

	/// <summary>
	/// CHANGED-ADDRESS attribute containing the changed transport address (RFC 3489, deprecated).
	/// </summary>
	ChangedAddress = 0x0005,

	/// <summary>
	/// USERNAME attribute used for message integrity (RFC 5389 / RFC 8489).
	/// </summary>
	Username = 0x0006,

	/// <summary>
	/// PASSWORD attribute (RFC 3489, deprecated).
	/// </summary>
	Password = 0x0007,

	/// <summary>
	/// MESSAGE-INTEGRITY attribute containing an HMAC-SHA1 hash for message authentication (RFC 5389 / RFC 8489).
	/// </summary>
	MessageIntegrity = 0x0008,

	/// <summary>
	/// ERROR-CODE attribute containing an error response code and reason phrase (RFC 5389 / RFC 8489).
	/// </summary>
	ErrorCode = 0x0009,

	/// <summary>
	/// UNKNOWN-ATTRIBUTES attribute listing attributes not understood by the server (RFC 5389 / RFC 8489).
	/// </summary>
	UnknownAttribute = 0x000A,

	/// <summary>
	/// REFLECTED-FROM attribute indicating the source of the request (RFC 3489, deprecated).
	/// </summary>
	ReflectedFrom = 0x000B,

	/// <summary>
	/// REALM attribute used for long-term credential authentication (RFC 5389 / RFC 8489).
	/// </summary>
	Realm = 0x0014,

	/// <summary>
	/// NONCE attribute used for long-term credential authentication (RFC 5389 / RFC 8489).
	/// </summary>
	Nonce = 0x0015,

	/// <summary>
	/// MESSAGE-INTEGRITY-SHA256 attribute containing an HMAC-SHA256 hash for message authentication (RFC 8489).
	/// </summary>
	MessageIntegritySha256 = 0x001C,

	/// <summary>
	/// PASSWORD-ALGORITHM attribute indicating the algorithm used for the password (RFC 8489).
	/// </summary>
	PasswordAlgorithm = 0x001D,

	/// <summary>
	/// USERHASH attribute containing a hash of the username for privacy (RFC 8489).
	/// </summary>
	UserHash = 0x001E,

	/// <summary>
	/// XOR-MAPPED-ADDRESS attribute containing the XOR-obfuscated mapped address (RFC 5389 / RFC 8489).
	/// </summary>
	XorMappedAddress = 0x0020,

	/// <summary>
	/// PADDING attribute used to align STUN messages on 4-byte boundaries (RFC 5780).
	/// </summary>
	Padding = 0x0026,

	/// <summary>
	/// RESPONSE-PORT attribute specifying the port for the response (RFC 5780).
	/// </summary>
	ResponsePort = 0x0027,

	/// <summary>
	/// PASSWORD-ALGORITHMS attribute listing supported password algorithms (RFC 8489).
	/// </summary>
	PasswordAlgorithms = 0x8002,

	/// <summary>
	/// ALTERNATE-DOMAIN attribute indicating an alternate domain for the server (RFC 8489).
	/// </summary>
	AlternateDomain = 0x8003,

	/// <summary>
	/// SOFTWARE attribute containing a description of the software being used (RFC 5389 / RFC 8489).
	/// </summary>
	Software = 0x8022,

	/// <summary>
	/// ALTERNATE-SERVER attribute indicating an alternate server address (RFC 5389 / RFC 8489).
	/// </summary>
	AlternateServer = 0x8023,

	/// <summary>
	/// CACHE-TIMEOUT attribute specifying how long the client may cache the response (RFC 5780).
	/// </summary>
	CacheTimeout = 0x8027,

	/// <summary>
	/// FINGERPRINT attribute containing a CRC-32 checksum of the STUN message (RFC 5389 / RFC 8489).
	/// </summary>
	Fingerprint = 0x8028,

	/// <summary>
	/// RESPONSE-ORIGIN attribute containing the source address of the response (RFC 5780).
	/// </summary>
	ResponseOrigin = 0x802B,

	/// <summary>
	/// OTHER-ADDRESS attribute containing the alternate server address (RFC 5780).
	/// </summary>
	OtherAddress = 0x802C,
}
