using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CASC.NET.Utils.IO;

namespace CASC.NET.Utils.Extensions
{
    internal static unsafe class IOExtensions
    {
        private static readonly uint[] _lookup32 = CreateLookup32();

        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (var i = 0; i < 256; i++)
            {
                var s = i.ToString("x2");
                result[i] = s[0] + ((uint)s[1] << 16);
            }
            return result;
        }

        public static string ToHexString(this byte[] bytes)
        {
            var lookup32 = _lookup32;
            var result = new char[bytes.Length * 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return new string(result);
        }

        private static int GetHexVal(char hex)
        {
            // For uppercase A-F letters:
            // return val - (val < 58 ? 48 : 55);
            // For lowercase a-f letters:
            return hex - (hex < 58 ? 48 : 87);
            // Or the two combined, but a bit slower:
            // return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        public static byte[] ToByteArray(this string hex) => ToByteArray(hex, hex.Length >> 1);

        public static byte[] ToByteArray(this string hex, int count)
        {
            count = Math.Min(hex.Length >> 1, count);
            var arr = new byte[count];
            for (var i = 0; i < count; ++i)
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));

            return arr;
        }

        /// <summary>
        /// Reads from a stream until the delimiter string is met.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        public static string ReadUntil(this TextReader reader, string delimiter)
        {
            var buffer = new StringBuilder();
            var delim_buffer = new CircularBuffer<char>(delimiter.Length);

            try
            {
                while (true)
                {
                    var c = (char)reader.Read();
                    delim_buffer.Enqueue(c);
                    if (delim_buffer.ToString() == delimiter)
                    {
                        if (buffer.Length > 0)
                            return buffer.ToString();
                        continue;
                    }
                    buffer.Append(c);
                }
            }
            catch (IOException /* ioe */)
            {
                return buffer.ToString();
            }
        }

        private class CircularBuffer<T> : Queue<T>
        {
            private int _capacity;

            public CircularBuffer(int capacity)
                : base(capacity)
            {
                _capacity = capacity;
            }

            public new void Enqueue(T item)
            {
                if (Count == _capacity)
                    Dequeue();
                base.Enqueue(item);
            }

            public override string ToString()
            {
                var items = this.Select(x => x.ToString()).ToList();
                return string.Join("", items);
            }
        }

        internal static T[] ReadArray<T>(this EndianBinaryReader br, long addr, long count) where T : struct
        {
            br.BaseStream.Seek(addr, SeekOrigin.Begin);
            return br.ReadArray<T>(count);
        }
        
        internal static string ReadCString(this BinaryReader br, int maxLength = -1)
        {
            var stringLength = 0;
            while (br.ReadByte() != '\0' && (maxLength == -1 || stringLength < maxLength))
                ++stringLength;

            br.BaseStream.Position -= stringLength + 1; // \0 was read as well
            return System.Text.Encoding.UTF8.GetString(br.ReadBytes(stringLength + 1), 0, stringLength); // Read again, but skip null terminator.
        }

        internal static T[] ReadArray<T>(this EndianBinaryReader br, long count) where T : struct
        {
            if (count == 0)
                return new T[0];

            if (SizeCache<T>.TypeRequiresMarshal)
                throw new ArgumentException(
                    "Cannot read a generic structure type that requires marshaling support. Read the structure out manually.");

            // NOTE: this may be safer to just call Read<T> each iteration to avoid possibilities of moved memory, etc.
            // For now, we'll see if this works.
            var ret = new T[count];
            fixed (byte* pB = br.ReadBytes(SizeCache<T>.Size * (int)count))
            {
                var genericPtr = (byte*)SizeCache<T>.GetUnsafePtr(ref ret[0]);
                UnsafeNativeMethods.CopyMemory(genericPtr, pB, SizeCache<T>.Size * (int)count);
            }
            return ret;
        }

        internal static void ReadToArray<T>(this EndianBinaryReader br, T[] data) where T : struct
        {
            if (SizeCache<T>.TypeRequiresMarshal)
                throw new ArgumentException(
                    "Cannot read a generic structure type that requires marshaling support. Read the structure out manually.");

            // NOTE: this may be safer to just call Read<T> each iteration to avoid possibilities of moved memory, etc.
            // For now, we'll see if this works.
            fixed (byte* pB = br.ReadBytes(SizeCache<T>.Size * data.Length))
            {
                for (var i = 0; i < data.Length; i++)
                {
                    var tPtr = (byte*)SizeCache<T>.GetUnsafePtr(ref data[i]);
                    UnsafeNativeMethods.CopyMemory(tPtr, &pB[i * SizeCache<T>.Size], SizeCache<T>.Size);
                }
            }
        }

        public static T EndianReverse<T>(this T value) where T : struct
        {
            if (SizeCache<T>.TypeRequiresMarshal)
                throw new ArgumentException("Cannot reverse endianness on a type that requires marshaling support");

            var valueData = (byte*) SizeCache<T>.GetUnsafePtr(ref value);
            var buf = new byte[SizeCache<T>.Size];
            fixed (byte* ptr = buf)
            {
                UnsafeNativeMethods.CopyMemory(ptr, valueData, buf.Length);
                for (var i = 0; i < SizeCache<T>.Size; ++i)
                    valueData[i] = buf[SizeCache<T>.Size - i - 1];
            }
            return value;
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerable)
        {
            var enumereator = enumerable.GetEnumerator();
            while (enumereator.MoveNext())
            {
                var innerEnum = enumereator.Current.GetEnumerator();
                while (innerEnum.MoveNext())
                    yield return innerEnum.Current;
            }
        }

        //! TODO Make this work for any level of nesting
        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerable, Func<T, bool> filter)
        {
            var enumereator = enumerable.GetEnumerator();
            while (enumereator.MoveNext())
            {
                var innerEnum = enumereator.Current.GetEnumerator();
                while (innerEnum.MoveNext())
                    if (filter(innerEnum.Current))
                        yield return innerEnum.Current;
            }
        }
    }
}
