namespace STUN.Messages.StunAttributeValues;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5780#section-7.2
/// </summary>
public class ChangeRequestStunAttributeValue : IStunAttributeValue
{
	/// <summary>
	/// Gets or sets a value indicating whether to request a response from a different IP address.
	/// </summary>
	public bool ChangeIp { get; set; }

	/// <summary>
	/// Gets or sets a value indicating whether to request a response from a different port.
	/// </summary>
	public bool ChangePort { get; set; }

	/// <summary>
	/// Serializes this CHANGE-REQUEST attribute value into the specified buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer.</param>
	/// <returns>The number of bytes written.</returns>
	public int WriteTo(Span<byte> buffer)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, 4, nameof(buffer));

		buffer[0] = buffer[1] = buffer[2] = 0;

		buffer[3] = (byte)(Convert.ToInt32(ChangeIp) << 2 | Convert.ToInt32(ChangePort) << 1);

		return 4;
	}

	/// <summary>
	/// Attempts to parse a CHANGE-REQUEST attribute value from the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw attribute value bytes.</param>
	/// <returns><see langword="true"/> if the value was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public bool TryParse(ReadOnlySpan<byte> buffer)
	{
		if (buffer.Length != 4)
		{
			return false;
		}

		ChangeIp = Convert.ToBoolean(buffer[3] >> 2 & 1);
		ChangePort = Convert.ToBoolean(buffer[3] >> 1 & 1);

		return true;
	}
}
