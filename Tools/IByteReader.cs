using System;

namespace Maple2.PacketLib.Tools {
    public interface IByteReader : IDisposable {
        int Available { get; }

        T Read<T>() where T : struct;
        T Peek<T>() where T : struct;
        byte[] ReadBytes(int count);
        bool ReadBool();
        byte ReadByte();
        short ReadShort();
        int ReadInt();
        float ReadFloat();
        long ReadLong();
        string ReadString();
        string ReadUnicodeString();
        void Skip(int count);
    }
}