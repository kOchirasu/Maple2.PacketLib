namespace MaplePacketLib2.Crypto {

    // Reverses the source data
    public class RearrangeCrypter : ICrypter {
        public void Init(uint version) { }

        public void Encrypt(byte[] src, uint seqKey) {
            int len = src.Length >> 1;
            for (int i = 0; i < len; i++) {
                byte data = src[i];

                src[i] = src[i + len];
                src[i + len] = data;
            }
        }

        public void Decrypt(byte[] src, uint seqKey) {
            int len = src.Length >> 1;
            for (int i = 0; i < len; i++) {
                byte data = src[i];

                src[i] = src[i + len];
                src[i + len] = data;
            }
        }
    }
}
