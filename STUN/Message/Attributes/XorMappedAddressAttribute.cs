using STUN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace STUN.Message.Attributes
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc5389#section-15.2
    /// </summary>
    public class XorMappedAddressAttribute : AddressAttribute
    {
        private readonly byte[] _magicCookie;
        private readonly byte[] _transactionId;

        public XorMappedAddressAttribute(byte[] magicCookie, byte[] transactionId)
        {
            _magicCookie = magicCookie;
            _transactionId = transactionId;
        }

        public override IEnumerable<byte> Bytes
        {
            get
            {
                if (Address == null)
                {
                    return Array.Empty<byte>();
                }

                var res = new List<byte> { 0, (byte)Family };
                res.AddRange(Xor(Port).ToBe());
                res.AddRange(Xor(Address).GetAddressBytes());
                return res;
            }
        }

        public override bool TryParse(byte[] bytes)
        {
            if (!base.TryParse(bytes)) return false;

            Port = Xor(Port);

            Address = Xor(Address);

            return true;
        }

        private ushort Xor(ushort port)
        {
            var b = port.ToBe().ToArray();
            var xPort = BitUtils.FromBe((byte)(_magicCookie[0] ^ b[0]), (byte)(_magicCookie[1] ^ b[1]));
            return xPort;
        }

        private IPAddress Xor(IPAddress address)
        {
            var b = address.GetAddressBytes();
            var xor = _magicCookie.Concat(_transactionId).ToArray();
            for (var i = 0; i < b.Length; ++i)
            {
                b[i] ^= xor[i];
            }
            return new IPAddress(b);
        }
    }
}
