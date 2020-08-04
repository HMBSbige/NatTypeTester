using System.Collections;
using System.Collections.Generic;

namespace STUN.Message.Attributes
{
    /// <summary>
    /// https://tools.ietf.org/html/rfc5780#section-7.2
    /// </summary>
    public class ChangeRequestAttribute : IAttribute
    {
        public IEnumerable<byte> Bytes
        {
            get
            {
                var bits = new BitArray(32, false) { [29] = ChangeIp, [30] = ChangePort };
                var res = new byte[4];
                bits.CopyTo(res, 0);
                return res;
            }
        }

        public bool ChangeIp { get; set; }

        public bool ChangePort { get; set; }

        public bool TryParse(byte[] bytes)
        {
            if (bytes.Length != 4) return false;

            var bits = new BitArray(bytes);

            ChangeIp = bits[29];
            ChangePort = bits[30];

            return true;
        }
    }
}