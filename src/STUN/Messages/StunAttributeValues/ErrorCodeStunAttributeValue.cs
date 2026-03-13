using System.Text;

namespace STUN.Messages.StunAttributeValues;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5389#section-15.6
/// </summary>
public class ErrorCodeStunAttributeValue : IStunAttributeValue
{
	/// <summary>
	/// Gets or sets the error code (e.g. 420, 500).
	/// </summary>
	public ushort ErrorCode { get; set; }

	/// <summary>
	/// Gets or sets the human-readable reason phrase for the error.
	/// </summary>
	public string ReasonPhrase { get; set; } = string.Empty;

	/// <summary>
	/// Gets the error class digit (hundreds digit of the error code).
	/// </summary>
	public byte Class => (byte)(ErrorCode % 1000 / 100);

	/// <summary>
	/// Gets the error number (last two digits of the error code).
	/// </summary>
	public byte Number => (byte)(ErrorCode % 100);

	/// <summary>
	/// The maximum byte length of the UTF-8 encoded reason phrase.
	/// </summary>
	public const int MaxReasonPhraseBytesLength = 762;

	/// <summary>
	/// Serializes this ERROR-CODE attribute value into the specified buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer.</param>
	/// <returns>The number of bytes written.</returns>
	public int WriteTo(Span<byte> buffer)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, 4, nameof(buffer));

		buffer[0] = buffer[1] = 0;
		buffer[2] = Class;
		buffer[3] = Number;

		int length = Encoding.UTF8.GetBytes(ReasonPhrase, buffer[4..]);

		return 4 + Math.Min(length, MaxReasonPhraseBytesLength);
	}

	/// <summary>
	/// Attempts to parse an ERROR-CODE attribute value from the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw attribute value bytes.</param>
	/// <returns><see langword="true"/> if the value was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public bool TryParse(ReadOnlySpan<byte> buffer)
	{
		if (buffer.Length is < 4 or > 4 + MaxReasonPhraseBytesLength)
		{
			return false;
		}

		byte @class = (byte)(buffer[2] & 0b111);
		ushort number = Math.Min(buffer[3], (ushort)99);

		ErrorCode = (ushort)(@class * 100 + number);

		ReasonPhrase = Encoding.UTF8.GetString(buffer[4..]);

		return true;
	}
}
