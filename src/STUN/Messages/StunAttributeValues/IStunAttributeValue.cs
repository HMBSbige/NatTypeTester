namespace STUN.Messages.StunAttributeValues;

/// <summary>
/// Defines the interface for STUN attribute values that can be serialized and parsed.
/// </summary>
public interface IStunAttributeValue
{
	/// <summary>
	/// Serializes this attribute value into the specified buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer.</param>
	/// <returns>The number of bytes written.</returns>
	int WriteTo(Span<byte> buffer);

	/// <summary>
	/// Attempts to parse this attribute value from the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw attribute value bytes.</param>
	/// <returns><see langword="true"/> if the value was parsed successfully; otherwise, <see langword="false"/>.</returns>
	bool TryParse(ReadOnlySpan<byte> buffer);
}
