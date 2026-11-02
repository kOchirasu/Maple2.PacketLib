using System;

namespace Maple2.PacketLib.Tools {
    public interface IByteReader : IDisposable {
        public int Available { get; }

        public T Read<T>() where T : struct;
        public T Peek<T>() where T : struct;
        public byte[] ReadBytes(int count);
        public void ReadBytes(scoped Span<byte> destination);
        public ReadOnlySpan<byte> ReadSpan(int count);
        public bool ReadBool();
        public byte ReadByte();
        public short ReadShort();
        public int ReadInt();
        public float ReadFloat();
        public long ReadLong();
        public string ReadString();
        public string ReadRawString(int length);
        public string ReadUnicodeString();
        public string ReadRawUnicodeString(int length);
        public void Skip(int count);
    }
}
