using System;

namespace CASC.NET.Utils
{
    public static class FastMarshal
    {
        public static unsafe void CopyMemory<T>(T[] to, IntPtr @from, int count) where T : struct
        {
            UnsafeNativeMethods.CopyMemory((byte*) SizeCache<T>.GetUnsafePtr(ref to[0]), (byte*) @from, count * SizeCache<T>.Size);
        }

        public static T[] CopyToManaged<T>(this IntPtr @from, int count) where T : struct
        {
            if (count == 0)
                return new T[0];

            var instance = new T[count];
            CopyMemory(instance, from, count);
            return instance;
        }

        public static unsafe IntPtr CopyToUnmanaged<T>(this T[] @from, int count) where T : struct
        {
            return count == 0 ? IntPtr.Zero : new IntPtr(SizeCache<T>.GetUnsafePtr(ref @from[0]));
        }
    }
}
