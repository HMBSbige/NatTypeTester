namespace STUN.Enums;

/// <summary>
/// STUN Attribute Registry
/// </summary>
/// <remarks>
/// https://tools.ietf.org/html/rfc3489#section-11.2
/// https://tools.ietf.org/html/rfc5389#section-18.2
/// https://tools.ietf.org/html/rfc5780#section-9.1
/// https://tools.ietf.org/html/rfc8489#section-18.3
/// </remarks>
public enum AttributeType : ushort
{
	Useless = 0x0000,
	MappedAddress = 0x0001,
	ResponseAddress = 0x0002,
	ChangeRequest = 0x0003,
	SourceAddress = 0x0004,
	ChangedAddress = 0x0005,
	Username = 0x0006,
	Password = 0x0007,
	MessageIntegrity = 0x0008,
	ErrorCode = 0x0009,
	UnknownAttribute = 0x000A,
	ReflectedFrom = 0x000B,
	Realm = 0x0014,
	Nonce = 0x0015,
	MessageIntegritySha256 = 0x001C,
	PasswordAlgorithm = 0x001D,
	UserHash = 0x001E,
	XorMappedAddress = 0x0020,
	Padding = 0x0026,
	ResponsePort = 0x0027,
	PasswordAlgorithms = 0x8002,
	AlternateDomain = 0x8003,
	Software = 0x8022,
	AlternateServer = 0x8023,
	CacheTimeout = 0x8027,
	Fingerprint = 0x8028,
	ResponseOrigin = 0x802B,
	OtherAddress = 0x802C,
}
