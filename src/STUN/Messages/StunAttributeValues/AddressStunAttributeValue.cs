using STUN.Enums;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace STUN.Messages.StunAttributeValues;

/// <summary>
/// https://datatracker.ietf.org/doc/html/rfc5389#section-15.1
/// </summary>
public abstract class AddressStunAttributeValue : IStunAttributeValue
{
	/// <summary>
	/// Gets or sets the IP address family (IPv4 or IPv6).
	/// </summary>
	public IpFamily Family { get; set; }

	/// <summary>
	/// Gets or sets the port number.
	/// </summary>
	public ushort Port { get; set; }

	/// <summary>
	/// Gets or sets the IP address.
	/// </summary>
	public IPAddress? Address { get; set; }

	/// <summary>
	/// Serializes this address attribute value into the specified buffer.
	/// </summary>
	/// <param name="buffer">The destination buffer.</param>
	/// <returns>The number of bytes written.</returns>
	public virtual int WriteTo(Span<byte> buffer)
	{
		IPAddress address = Address ?? throw new InvalidOperationException(@"You should set Address info!");

		ArgumentOutOfRangeException.ThrowIfLessThan(buffer.Length, 4 + 4, nameof(buffer));

		buffer[0] = 0;
		buffer[1] = (byte)Family;
		BinaryPrimitives.WriteUInt16BigEndian(buffer[2..], Port);
		if (!address.TryWriteBytes(buffer[4..], out int bytesWritten))
		{
			throw new ArgumentException(@"Buffer is too small.", nameof(buffer));
		}

		return 4 + bytesWritten;
	}

	/// <summary>
	/// Attempts to parse an address attribute value from the specified buffer.
	/// </summary>
	/// <param name="buffer">The buffer containing the raw attribute value bytes.</param>
	/// <returns><see langword="true"/> if the value was parsed successfully; otherwise, <see langword="false"/>.</returns>
	public virtual bool TryParse(ReadOnlySpan<byte> buffer)
	{
		int length = 4;

		if (buffer.Length < length)
		{
			return false;
		}

		Family = (IpFamily)buffer[1];

		switch (Family)
		{
			case IpFamily.IPv4:
				length += 4;
				break;
			case IpFamily.IPv6:
				length += 16;
				break;
			default:
				return false;
		}

		if (buffer.Length != length)
		{
			return false;
		}

		Port = BinaryPrimitives.ReadUInt16BigEndian(buffer[2..]);

		Address = new IPAddress(buffer[4..]);

		return true;
	}

	/// <summary>
	/// Returns a string representation of the address and port in standard notation.
	/// </summary>
	/// <returns>The address and port formatted as <c>address:port</c> for IPv4 or <c>[address]:port</c> for IPv6.</returns>
	public override string? ToString()
	{
		return Address?.AddressFamily switch
		{
			AddressFamily.InterNetwork => $@"{Address}:{Port}",
			AddressFamily.InterNetworkV6 => $@"[{Address}]:{Port}",
			_ => base.ToString()
		};
	}
}
