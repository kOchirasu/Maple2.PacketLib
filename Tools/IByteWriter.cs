using System;

namespace Maple2.PacketLib.Tools {
    public interface IByteWriter : IDisposable {
        void Write<T>(in T value) where T : struct;
        void WriteBytes(byte[] value);
        void WriteBytes(byte[] value, int offset, int length);
        void WriteBool(bool value);
        void WriteByte(byte value = 0);
        void WriteShort(short value = 0);
        void WriteInt(int value = 0);
        void WriteFloat(float value = 0f);
        void WriteLong(long value = 0);
        void WriteString(string value = "");
        void WriteUnicodeString(string value = "");
    }
}