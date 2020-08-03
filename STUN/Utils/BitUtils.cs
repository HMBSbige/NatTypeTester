using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace STUN.Utils
{
    public static class BitUtils
    {
        public static IEnumerable<byte> ToBe(this ushort num)
        {
            var res = BitConverter.GetBytes(num);
            return BitConverter.IsLittleEndian ? res.Reverse() : res;
        }

        public static ushort FromBe(byte b1, byte b2)
        {
            return BitConverter.ToUInt16(BitConverter.IsLittleEndian ? new[] { b2, b1 } : new[] { b1, b2 }, 0);
        }

        public static IEnumerable<byte> GetRandomBytes(int n)
        {
            var temp = new byte[n];
            using var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(temp);
            return temp;
        }

        public static bool IsEqual(this byte[] a, byte[] b)
        {
            return a != null && b != null && a.SequenceEqual(b);
        }
    }
}
