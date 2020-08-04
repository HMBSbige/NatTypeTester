using STUN.Message.Enums;
using STUN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace STUN.Message.Attributes
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc5389#section-15.1
    /// </summary>
    public class MappedAddressAttribute : IAttribute
    {
        public virtual IEnumerable<byte> Bytes
        {
            get
            {
                if (Address == null)
                {
                    return Array.Empty<byte>();
                }
                var res = new List<byte> { 0, (byte)Family };
                res.AddRange(Port.ToBe());
                res.AddRange(Address.GetAddressBytes());
                return res;
            }
        }

        public IpFamily Family { get; set; }

        public ushort Port { get; set; }

        public IPAddress Address { get; set; }

        public virtual bool TryParse(byte[] bytes)
        {
            var length = 4;

            if (bytes.Length < length) return false;

            Family = (IpFamily)bytes[1];

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

            if (bytes.Length == length) return false;

            Port = BitUtils.FromBe(bytes[2], bytes[3]);

            Address = new IPAddress(bytes.Skip(4).ToArray());

            return true;
        }
    }
}