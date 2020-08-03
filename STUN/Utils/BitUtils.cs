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

        public static IEnumerable<byte> GetRandomBytes(int n)
        {
            var temp = new byte[n];
            using var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(temp);
            return temp;
        }
    }
}
