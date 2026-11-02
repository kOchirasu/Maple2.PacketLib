using System;

namespace Maple2.PacketLib.Tools {
    public interface IByteWriter : IDisposable {
        public int Length { get; }

        public void Write<T>(scoped in T value) where T : struct;
        public void WriteBytes(byte[] value);
        public void WriteBytes(byte[] value, int offset, int length);
        public void WriteBytes(scoped ReadOnlySpan<byte> value);
        public void WriteBool(bool value);
        public void WriteByte(byte value = 0);
        public void WriteShort(short value = 0);
        public void WriteInt(int value = 0);
        public void WriteFloat(float value = 0f);
        public void WriteLong(long value = 0);
        public void WriteString(string value = "");
        public void WriteString(scoped ReadOnlySpan<char> value);
        public void WriteRawString(scoped ReadOnlySpan<char> value);
        public void WriteUnicodeString(string value = "");
        public void WriteUnicodeString(scoped ReadOnlySpan<char> value);
        public void WriteRawUnicodeString(scoped ReadOnlySpan<char> value);
        public byte[] ToArray();
    }
}
