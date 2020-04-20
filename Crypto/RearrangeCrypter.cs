namespace MaplePacketLib2.Crypto {

    // Reverses the source data
    public class RearrangeCrypter : ICrypter {
        private const int INDEX = 1;

        public static uint GetIndex(uint version) {
            return (version + INDEX) % 3 + 1;
        }

        public void Encrypt(byte[] src) {
            Encrypt(src, 0, src.Length);
        }

        public void Encrypt(byte[] src, int offset, int count) {
            int len = count >> 1;
            for (int i = offset; i < len; i++) {
                byte swap = src[i];
                src[i] = src[i + len];
                src[i + len] = swap;
            }
        }

        public void Decrypt(byte[] src) {
            Decrypt(src, 0, src.Length);
        }

        public void Decrypt(byte[] src, int offset, int count) {
            int len = count >> 1;
            for (int i = offset; i < len; i++) {
                byte swap = src[i];
                src[i] = src[i + len];
                src[i + len] = swap;
            }
        }
    }
}
