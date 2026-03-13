namespace STUN.Messages.StunAttributeValues;

/// <summary>
/// 无法理解的属性
/// </summary>
public class UselessStunAttributeValue : IStunAttributeValue
{
	/// <summary>
	/// Writing is not supported for unrecognized attribute values.
	/// </summary>
	/// <param name="buffer">The destination buffer.</param>
	/// <returns>Does not return; always throws <see cref="NotSupportedException"/>.</returns>
	/// <exception cref="NotSupportedException">Always thrown.</exception>
	public int WriteTo(Span<byte> buffer)
	{
		throw new NotSupportedException();
	}

	/// <summary>
	/// Parses the attribute value, always succeeding since the content is ignored.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw attribute value bytes.</param>
	/// <returns>Always returns <see langword="true"/>.</returns>
	public bool TryParse(ReadOnlySpan<byte> buffer)
	{
		return true;
	}
}
