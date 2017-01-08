namespace CASC.NET.Utils.IO
{
    /// <summary>
    /// Implementation of EndianBitConverter which converts to/from big-endian
    /// byte arrays.
    /// </summary>
    internal sealed class BigEndianBitConverter : EndianBitConverter
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
        public override bool IsLittleEndian() => false;

        /// <summary>
        /// Indicates the byte order ("endianess") in which data is converted using this class.
        /// </summary>
        public override Endianness Endianness => Endianness.BigEndian;

        /// <summary>
        /// Copies the specified number of bytes from value to buffer, starting at index.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <param name="bytes">The number of bytes to copy</param>
        /// <param name="buffer">The buffer to copy the bytes into</param>
        /// <param name="index">The index to start at</param>
        protected override unsafe void CopyBytesImpl(long value, int bytes, byte[] buffer, int index)
        {
            // Not checked - no write in this lib
            var valuePtr = (byte*)SizeCache<long>.GetUnsafePtr(ref value);
            for (var i = 0; i < bytes; ++i)
                buffer[i + index + bytes - 1] = valuePtr[i];

            /*var endOffset = index + bytes - 1;
            for (var i = 0; i < bytes; i++)
            {
                buffer[endOffset - i] = unchecked((byte)(value & 0xff));
                value = value >> 8;
            }*/
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
            var fixedRet = (byte*) SizeCache<long>.GetUnsafePtr(ref ret);
            for (var i = 0; i < bytesToConvert; ++i)
                fixedRet[i] = buffer[startIndex + bytesToConvert - i - 1];

            return ret;
        }
    }
}
