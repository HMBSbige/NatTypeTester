using STUN.Message.Attributes;
using STUN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using STUN.Enums;

namespace STUN.Message
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc5389#section-15
    /// </summary>
    public class Attribute
    {
        /*
            Length 是大端
            必须4字节对齐
            对齐的字节可以是任意值
             0                   1                   2                   3
             0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            |         Type                  |            Length             |
            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            |                         Value (variable)                ....
            +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
         */

        public AttributeType Type { get; set; } = AttributeType.Useless;

        public ushort Length { get; set; }

        public int RealLength => Type == AttributeType.Useless ? 0 : 4 + Length + (4 - Length % 4) % 4;

        public IAttribute Value { get; set; }

        private readonly byte[] _magicCookie;
        private readonly byte[] _transactionId;

        public Attribute() { }

        public Attribute(byte[] magicCookie, byte[] transactionId)
        {
            if (magicCookie.Length != 4 || transactionId.Length != 12)
            {
                throw new ArgumentException(@"Wrong Transaction ID length");
            }

            _magicCookie = magicCookie;

            _transactionId = transactionId;
        }

        public IEnumerable<byte> ToBytes()
        {
            var res = new List<byte>();

            res.AddRange(Convert.ToUInt16(Type).ToBe());
            res.AddRange(Length.ToBe());
            res.AddRange(Value.Bytes);

            var n = (4 - res.Count % 4) % 4; // 填充的字节数
            res.AddRange(BitUtils.GetRandomBytes(n));

            return res;
        }

        /// <returns>
        /// Parse 成功字节，0 则表示 Parse 失败
        /// </returns>
        public int TryParse(byte[] bytes)
        {
            if (bytes.Length < 4) return 0;

            Type = (AttributeType)BitUtils.FromBe(bytes[0], bytes[1]);

            Length = BitUtils.FromBe(bytes[2], bytes[3]);

            if (bytes.Length < 4 + Length) return 0;

            var value = bytes.Skip(4).Take(Length).ToArray();

            IAttribute t = Type switch
            {
                AttributeType.MappedAddress => new MappedAddressAttribute(),
                AttributeType.XorMappedAddress => new XorMappedAddressAttribute(_magicCookie, _transactionId),
                AttributeType.ResponseAddress => new ResponseAddressAttribute(),
                AttributeType.ChangeRequest => new ChangeRequestAttribute(),
                AttributeType.SourceAddress => new SourceAddressAttribute(),
                AttributeType.ChangedAddress => new ChangedAddressAttribute(),
                AttributeType.OtherAddress => new OtherAddressAttribute(),
                AttributeType.ReflectedFrom => new ReflectedFromAttribute(),
                AttributeType.ErrorCode => new ErrorCodeAttribute(),
                //TODO:more
                _ => new UselessAttribute()
            };
            Value = t.TryParse(value) ? t : null;

            return 4 + Length + (4 - Length % 4) % 4; // 对齐
        }
    }
}
