namespace STUN.Enums;

internal enum Class : ushort
{
	Request = 0b00000_0_000_0_0000,
	Indication = 0b00000_0_000_1_0000,
	SuccessResponse = 0b00000_1_000_0_0000,
	ErrorResponse = 0b00000_1_000_1_0000,
}
