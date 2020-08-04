using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace STUN.Message.Attributes
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc5389#section-15.6
    /// </summary>
    public class ErrorCodeAttribute : IAttribute
    {
        public IEnumerable<byte> Bytes
        {
            get
            {
                var res = new List<byte> { 0, 0, Class, Number };
                res.AddRange(Encoding.UTF8.GetBytes(ReasonPhrase).Take(MaxReasonPhraseBytesLength));
                return res;
            }
        }

        public ushort ErrorCode { get; set; }
        public string ReasonPhrase { get; set; }

        public byte Class => (byte)(ErrorCode % 1000 / 100);
        public byte Number => (byte)(ErrorCode % 100);

        public const int MaxReasonPhraseBytesLength = 762;

        public bool TryParse(byte[] bytes)
        {
            if (bytes.Length < 4 || bytes.Length > 4 + MaxReasonPhraseBytesLength) return false;

            var @class = (byte)(bytes[2] & 0b111);
            var number = Math.Min(bytes[3], (ushort)99);

            ErrorCode = (ushort)(@class * 100 + number);

            ReasonPhrase = bytes.Length > 4 ? Encoding.UTF8.GetString(bytes, 4, bytes.Length - 4) : string.Empty;

            return true;
        }
    }
}