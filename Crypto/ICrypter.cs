using System;

namespace Maple2.PacketLib.Crypto {
    public interface ICrypter {
        void Encrypt(byte[] src);
        void Encrypt(byte[] src, int start, int end);
        void Encrypt(Span<byte> src);
        void Decrypt(byte[] src);
        void Decrypt(byte[] src, int start, int end);
        void Decrypt(Span<byte> src);
    }
}
