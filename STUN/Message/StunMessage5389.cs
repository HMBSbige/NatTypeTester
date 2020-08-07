using STUN.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using STUN.Enums;

namespace STUN.Message
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc5389#section-6
    /// </summary>
    public class StunMessage5389
    {
        public IEnumerable<byte> Header =>
                Convert.ToUInt16(StunMessageType).ToBe().Concat(MessageLengthBytes)
                        .Concat(MagicCookieBytes).Concat(TransactionId);

        public IEnumerable<Attribute> Attributes { get; set; }

        #region Header

        public StunMessageType StunMessageType { get; set; }

        public ushort MessageLength => Attributes.Aggregate<Attribute, ushort>(0, (current, attribute) => (ushort)(current + Convert.ToUInt16(attribute.RealLength)));

        public IEnumerable<byte> MessageLengthBytes => MessageLength.ToBe();

        public int MagicCookie { get; set; }

        public IEnumerable<byte> MagicCookieBytes => MagicCookie.ToBe();

        public byte[] TransactionId { get; private set; }

        public IEnumerable<byte> ClassicTransactionId => MagicCookieBytes.Concat(TransactionId);

        #endregion

        public IEnumerable<byte> Bytes =>
                Attributes.Aggregate(Header, (current, attribute) => current.Concat(attribute.ToBytes()));

        public StunMessage5389()
        {
            Attributes = Array.Empty<Attribute>();
            StunMessageType = StunMessageType.BindingRequest;
            MagicCookie = 0x2112A442;
            TransactionId = BitUtils.GetRandomBytes(12).ToArray();
        }

        public bool TryParse(byte[] bytes)
        {
            if (bytes.Length < 20) return false; // Check length

            StunMessageType = (StunMessageType)BitUtils.FromBe((byte)(bytes[0] & 0b0011_1111), bytes[1]);

            if (!Enum.IsDefined(typeof(StunMessageType), StunMessageType)) return false;

            var length = BitUtils.FromBe(bytes[2], bytes[3]);

            MagicCookie = BitUtils.FromBeToInt(bytes.Skip(4).Take(4));

            TransactionId = bytes.Skip(8).Take(12).ToArray();

            if (bytes.Length != length + 20) return false; // Check length

            var list = new List<Attribute>();

            var b = bytes.Skip(20).ToArray();

            while (b.Length > 0)
            {
                var attribute = new Attribute(MagicCookieBytes.ToArray(), TransactionId);
                var offset = attribute.TryParse(b);
                if (offset > 0)
                {
                    list.Add(attribute);
                    b = b.Skip(offset).ToArray();
                }
                else
                {
                    Debug.WriteLine($@"[Warning] Ignore wrong attribute: {BitConverter.ToString(b)}");
                    break;
                }
            }

            Attributes = list;

            return true;
        }
    }
}
