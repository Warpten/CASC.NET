namespace CASC.NET.Utils.IO
{
    /// <summary>
    /// Endianness of a converter
    /// </summary>
    public enum Endianness
    {
        /// <summary>
        /// Little endian - least significant byte first
        /// </summary>
        LittleEndian,
        /// <summary>
        /// Big endian - most significant byte first
        /// </summary>
        BigEndian
    }

    /// <summary>
    /// Implementation of EndianBitConverter which converts to/from little-endian
    /// byte arrays.
    /// </summary>
    internal sealed class LittleEndianBitConverter : EndianBitConverter
    {
        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        /// <remarks>
        /// Different computer architectures store data using different byte orders. "Big-endian"
        /// means the most significant byte is on the left end of a word. "Little-endian" means the 
        /// most significant byte is on the right end of a word.
        /// </remarks>
        /// <returns>true if this converter is little-endian, false otherwise.</returns>
        public override bool IsLittleEndian() => true;

        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        public override Endianness Endianness => Endianness.LittleEndian;

        /// <summary>
        /// Copies the specified number of bytes from value to buffer, starting at index.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <param name="bytes">The number of bytes to copy</param>
        /// <param name="buffer">The buffer to copy the bytes into</param>
        /// <param name="index">The index to start at</param>
        protected override unsafe void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
        {
            fixed (byte* buf = buffer)
                UnsafeNativeMethods.CopyMemory((byte*)SizeCache<long>.GetUnsafePtr(ref value), buf + index, bytes);
        }

        /// <summary>
        /// Returns a value built from the specified number of bytes from the given buffer,
        /// starting at index.
        /// </summary>
        /// <param name="buffer">The data in byte array format</param>
        /// <param name="startIndex">The first index to use</param>
        /// <param name="bytesToConvert">The number of bytes to use</param>
        /// <returns>The value built from the given bytes</returns>
        protected override unsafe long FromBytes(byte[] buffer, int startIndex, int bytesToConvert)
        {
            long ret = 0;
            fixed (byte* buf = buffer)
                UnsafeNativeMethods.CopyMemory((byte*)SizeCache<long>.GetUnsafePtr(ref ret), buf + startIndex, bytesToConvert);
            return ret;
        }
    }
}
