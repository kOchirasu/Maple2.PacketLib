using MaplePacketLib2.Tools;
using System;

namespace MaplePacketLib2.Crypto {
    public class MapleCipher {
        private const int ENCRYPT_NONE = 0;
        private const int ENCRYPT_REARRANGE = 1;
        private const int ENCRYPT_XOR = 2;
        private const int ENCRYPT_TABLE = 3;
        private const int HEADER_SIZE = 6;

        private readonly ICrypter[] crypt;
        private readonly uint version;
        private readonly uint encSeq;
        private readonly uint decSeq;

        private uint iv;

        public Func<byte[], byte[]> Transform { get; private set; }

        private MapleCipher(uint version, uint iv, uint seqBlock) {
            this.version = version;
            this.iv = iv;

            encSeq = seqBlock;
            decSeq = ReverseDigits(seqBlock);

            // Initialize Crypter Order
            crypt = new ICrypter[4];
            crypt[ENCRYPT_NONE] = null;
            crypt[(version + ENCRYPT_REARRANGE) % 3 + 1] = new RearrangeCrypter();
            crypt[(version + ENCRYPT_XOR) % 3 + 1] = new XORCrypter(version);
            crypt[(version + ENCRYPT_TABLE) % 3 + 1] = new TableCrypter(version);
        }

        public static MapleCipher Encryptor(uint version, uint iv, uint seqBlock) {
            var cipher = new MapleCipher(version, iv, seqBlock);
            cipher.Transform = cipher.Encrypt;
            return cipher;
        }

        public static MapleCipher Decryptor(uint version, uint iv, uint seqBlock) {
            var cipher = new MapleCipher(version, iv, seqBlock);
            cipher.Transform = cipher.Decrypt;
            return cipher;
        }

        // Advances iv to skip packets
        public void Advance() {
            iv = Rand32.CrtRand(iv);
        }

        private byte[] Encrypt(byte[] packet) {
            ushort rawSeq = EncodeSeqBase(version, iv);
            Encrypt(packet, encSeq);
            iv = Rand32.CrtRand(iv);

            var writer = new PacketWriter(packet.Length + HEADER_SIZE);
            writer.Write(rawSeq);
            writer.Write(packet.Length);
            writer.Write(packet);

            return writer.Buffer;
        }

        private void Encrypt(byte[] packet, uint seqBlock) {
            while (seqBlock > 0) {
                ICrypter crypter = crypt[seqBlock % 10];
                crypter?.Encrypt(packet);
                seqBlock /= 10;
            }
        }

        private byte[] Decrypt(byte[] packet) {
            var reader = new PacketReader(packet);
            uint rawSeq = reader.Read<ushort>();
            if (DecodeSeqBase(rawSeq, iv) != version) {
                throw new ArgumentException("Packet has invalid sequence header.");
            }
            int packetSize = reader.Read<int>();
            if (packet.Length < packetSize + HEADER_SIZE) {
                throw new ArgumentException("Packet has invalid length.");
            }

            packet = reader.Read(packetSize);
            Decrypt(packet, decSeq);
            iv = Rand32.CrtRand(iv);

            return packet;
        }

        private void Decrypt(byte[] packet, uint seqBlock) {
            while (seqBlock > 0) {
                ICrypter crypter = crypt[seqBlock % 10];
                crypter?.Decrypt(packet);
                seqBlock /= 10;
            }
        }

        private static uint DecodeSeqBase(uint rawSeq, uint seqKey) {
            return ((seqKey >> 16) ^ rawSeq);
        }

        private static ushort EncodeSeqBase(uint version, uint seqKey) {
            return (ushort)(version ^ (seqKey >> 16));
        }

        private static uint ReverseDigits(uint n) {
            uint result = 0;
            while (n > 0) {
                result = result * 10 + n % 10;
                n /= 10;
            }
            return result;
        }
    }
}