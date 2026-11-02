using System;

namespace Maple2.PacketLib.Crypto {

    // Left rotates data left by length / 2
    public class RearrangeCrypter : ICrypter {
        private const int INDEX = 1;

        public static uint GetIndex(uint version) {
            return (version + INDEX) % 3 + 1;
        }

        public void Encrypt(byte[] src) {
            Encrypt(src.AsSpan());
        }

        public void Encrypt(byte[] src, int start, int end) {
            Encrypt(src.AsSpan(start, end - start));
        }

        public void Encrypt(Span<byte> src) {
            int len = src.Length >> 1;
            for (int i = 0; i < len; i++) {
                (src[i], src[i + len]) = (src[i + len], src[i]);
            }
        }

        public void Decrypt(byte[] src) {
            Decrypt(src.AsSpan());
        }

        public void Decrypt(byte[] src, int start, int end) {
            Decrypt(src.AsSpan(start, end - start));
        }

        public void Decrypt(Span<byte> src) {
            int len = src.Length >> 1;
            for (int i = 0; i < len; i++) {
                (src[i], src[i + len]) = (src[i + len], src[i]);
            }
        }
    }
}
