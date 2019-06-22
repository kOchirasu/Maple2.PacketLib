using System;

namespace MaplePacketLib2.Crypto {
    public class XORCrypter : ICrypter {
        private readonly byte[] shuffle;

        public XORCrypter() {
            this.shuffle = new byte[2];
        }

        public void Init(uint version) {
            var rand1 = new Rand32(version);
            var rand2 = new Rand32(2 * version);

            shuffle[0] = (byte)(rand1.RandomFloat() * 255.0f);
            shuffle[1] = (byte)(rand2.RandomFloat() * 255.0f);
        }

        public void Encrypt(byte[] src, uint seqKey) {
            int flag = 0;
            for (int i = 0; i < src.Length; i++) {
                src[i] ^= shuffle[flag];

                flag ^= 1;
            }
        }

        public void Decrypt(byte[] src, uint seqKey) {
            int flag = 0;
            for (int i = 0; i < src.Length; i++) {
                src[i] ^= shuffle[flag];

                flag ^= 1;
            }
        }
    }
}
