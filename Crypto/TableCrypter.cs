using System;

namespace MaplePacketLib2.Crypto {
    public class TableCrypter : ICrypter {
        private readonly byte[] decrypted;
        private readonly byte[] encrypted;

        public TableCrypter() {
            this.decrypted = new byte[256];
            this.encrypted = new byte[256];
        }

        public void Init(uint version) {
            int[] shuffle = new int[256];
            for (int i = 0; i < shuffle.Length; i++) {
                shuffle[i] = i;
            }

            var rand32 = new Rand32((uint)Math.Pow(version, 2));
            Shuffle(shuffle, rand32);

            // Shuffle the table of bytes
            for (int i = 0; i < shuffle.Length; i++) {
                encrypted[i] = (byte)(shuffle[i] & 0xFF);
                decrypted[encrypted[i] & 0xFF] = (byte)(i & 0xFF);
            }
        }

        public void Encrypt(byte[] src, uint seqKey) {
            for (int i = 0; i < src.Length; i++) {
                src[i] = encrypted[src[i] & 0xFF];
            }
        }

        public void Decrypt(byte[] src, uint seqKey) {
            for (int i = 0; i < src.Length; i++) {
                src[i] = decrypted[src[i] & 0xFF];
            }
        }

        private static void Shuffle(int[] data, Rand32 rand32) {
            for (int i = data.Length - 1; i >= 1; --i) {
                uint rand = (uint)(rand32.Random() % (i + 1));

                if (i == rand) continue;
                if (rand >= data.Length || i >= data.Length) {
                    return;
                }
                int val = data[i];

                data[i] = data[rand];
                data[rand] = val;
            }
        }
    }
}
