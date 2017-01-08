namespace CASC.NET.Structures
{
#pragma warning disable 0649
    public unsafe struct RootRecord
    {
        public fixed byte MD5[16];
        public ulong Hash;
    }

    public unsafe struct IndexRecord
    {
        public fixed byte HeaderHash[9];
        public fixed byte Data[40 / 8]; // uint10 DataNumber; uint30 Offset;
        public uint Size;
    }
#pragma warning restore 0649
}
