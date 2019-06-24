namespace MaplePacketLib2.Crypto {

    // Reverses the source data
    public class RearrangeCrypter : ICrypter {

        public void Encrypt(byte[] src) {
            int len = src.Length >> 1;
            for (int i = 0; i < len; i++) {
                byte swap = src[i];
                src[i] = src[i + len];
                src[i + len] = swap;
            }
        }

        public void Decrypt(byte[] src) {
            int len = src.Length >> 1;
            for (int i = 0; i < len; i++) {
                byte swap = src[i];
                src[i] = src[i + len];
                src[i + len] = swap;
            }
        }
    }
}
