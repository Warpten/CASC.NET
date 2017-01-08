using System;
using System.Security.Cryptography;

namespace CASC.NET.Utils
{
    // Implementation of Bob Jenkins' hash function in C# (96 bit internal state)
    public class JenkinsHashing : HashAlgorithm
    {
        public static JenkinsHashing Instance = new JenkinsHashing();

        private ulong hashValue;
        private static byte[] fakeHash = new byte[0];

        private static uint rot(uint x, int k)
        {
            return (x << k) | (x >> (32 - k));
        }

        public ulong ComputeHash(string path)
        {
            var tempstr = path.Replace('/', '\\').ToUpper();
            var data = System.Text.Encoding.ASCII.GetBytes(tempstr);
            ComputeHash(data);
            return hashValue;
        }

        public override void Initialize()
        {

        }

        protected override unsafe void HashCore(byte[] array, int ibStart, int cbSize)
        {
            var length = (uint)array.Length;
            var a = 0xdeadbeef + length;
            var b = a;
            var c = a;

            if (length == 0)
            {
                hashValue = ((ulong)c << 32) | b;
                return;
            }

            var newLen = (length + (12 - length % 12) % 12);

            if (length != newLen)
            {
                Array.Resize(ref array, (int)newLen);
                length = newLen;
            }

            fixed (byte* bb = array)
            {
                var u = (uint*)bb;

                for (var j = 0; j < length - 12; j += 12)
                {
                    a += *(u + j / 4);
                    b += *(u + j / 4 + 1);
                    c += *(u + j / 4 + 2);

                    a -= c; a ^= rot(c, 4); c += b;
                    b -= a; b ^= rot(a, 6); a += c;
                    c -= b; c ^= rot(b, 8); b += a;
                    a -= c; a ^= rot(c, 16); c += b;
                    b -= a; b ^= rot(a, 19); a += c;
                    c -= b; c ^= rot(b, 4); b += a;
                }

                var i = length - 12;
                a += *(u + i / 4);
                b += *(u + i / 4 + 1);
                c += *(u + i / 4 + 2);

                c ^= b; c -= rot(b, 14);
                a ^= c; a -= rot(c, 11);
                b ^= a; b -= rot(a, 25);
                c ^= b; c -= rot(b, 16);
                a ^= c; a -= rot(c, 4);
                b ^= a; b -= rot(a, 14);
                c ^= b; c -= rot(b, 24);

                hashValue = ((ulong)c << 32) | b;
            }
        }

        protected override byte[] HashFinal()
        {
            return fakeHash;
        }
    }
}
