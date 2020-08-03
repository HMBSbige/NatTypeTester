using System;
using System.Collections.Generic;
using STUN.Message.Enums;
using STUN.Utils;

namespace STUN.Message.Attributes
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

        public AttributeType Type { get; set; }

        public ushort Length { get; set; }

        public IAttribute Value { get; set; }

        public IEnumerable<byte> ToBytes()
        {
            var res = new List<byte>();

            res.AddRange(Convert.ToUInt16(Type).ToBe());
            res.AddRange(Length.ToBe());
            res.AddRange(Value.Value);

            var n = (4 - res.Count % 4) % 4; // 填充的字节数
            res.AddRange(BitUtils.GetRandomBytes(n));

            return res;
        }
    }
}
