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
	public IpFamily Family { get; set; }

	public ushort Port { get; set; }

	public IPAddress? Address { get; set; }

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
