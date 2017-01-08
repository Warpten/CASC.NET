using System;
using System.Collections.Generic;
using System.Linq;

namespace CASC.NET.Utils
{
    public class ByteArrayComparer : EqualityComparer<byte[]>
    {
        public static readonly ByteArrayComparer Instance = new ByteArrayComparer();

        public override bool Equals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
                return left == right;

            return left.SequenceEqual(right);
        }

        public override int GetHashCode(byte[] key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            var hash = key.Aggregate(2166136261, (current, keyByte) => (current ^ keyByte)*16777619);

            return unchecked((int)hash);
        }

        public static int Hash(byte[] key)
        {
            var hash = key.Aggregate(2166136261, (current, keyByte) => (current ^ keyByte) * 16777619);

            return unchecked((int)hash);
        }

        public static unsafe int Hash(byte* key, int length)
        {
            var hash = 2166136261;
            for (var i = 0; i < length; ++i)
                hash = (hash ^ key[i]) * 16777619;
            return unchecked((int)hash);
        }
    }
}
