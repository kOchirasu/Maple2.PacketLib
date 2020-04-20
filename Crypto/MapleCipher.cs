using MaplePacketLib2.Tools;
using System;
using System.Collections.Generic;

namespace MaplePacketLib2.Crypto {
    public class MapleCipher {
        private const int HEADER_SIZE = 6;

        private readonly uint version;
        private readonly ICrypter[] encryptSeq;
        private readonly ICrypter[] decryptSeq;

        private uint iv;

        public Func<byte[], int, int, Packet> Transform { get; private set; }

        private MapleCipher(uint version, uint iv, uint blockIV) {
            this.version = version;
            this.iv = iv;

            // Initialize Crypter Sequence
            List<ICrypter> cryptSeq = InitCryptSeq(version, blockIV);
            encryptSeq = cryptSeq.ToArray();
            cryptSeq.Reverse();
            decryptSeq = cryptSeq.ToArray();
        }

        public static MapleCipher Encryptor(uint version, uint iv, uint blockIV) {
            var cipher = new MapleCipher(version, iv, blockIV);
            cipher.Transform = cipher.Encrypt;
            return cipher;
        }

        public static MapleCipher Decryptor(uint version, uint iv, uint blockIV) {
            var cipher = new MapleCipher(version, iv, blockIV);
            cipher.Transform = cipher.Decrypt;
            return cipher;
        }

        public void AdvanceIV() {
            iv = Rand32.CrtRand(iv);
        }

        public Packet WriteHeader(byte[] packet) {
            ushort encSeq = EncodeSeqBase();

            var writer = new PacketWriter(packet.Length + HEADER_SIZE);
            writer.WriteUShort(encSeq);
            writer.WriteInt(packet.Length);
            writer.WriteBytes(packet);

            return writer;
        }

        public int ReadHeader(PacketReader packet) {
            ushort encSeq = packet.ReadUShort();
            ushort decSeq = DecodeSeqBase(encSeq);
            if (decSeq != version) {
                throw new ArgumentException($"Packet has invalid sequence header: {decSeq}");
            }
            int packetSize = packet.ReadInt();
            if (packet.Length < packetSize + HEADER_SIZE) {
                throw new ArgumentException($"Packet has invalid length: {packet.Length}");
            }

            return packetSize;
        }

        private static List<ICrypter> InitCryptSeq(uint version, uint blockIV) {
            ICrypter[] crypt = new ICrypter[4];
            crypt[RearrangeCrypter.GetIndex(version)] = new RearrangeCrypter();
            crypt[XORCrypter.GetIndex(version)] = new XORCrypter(version);
            crypt[TableCrypter.GetIndex(version)] = new TableCrypter(version);

            List<ICrypter> cryptSeq = new List<ICrypter>();
            while (blockIV > 0) {
                var crypter = crypt[blockIV % 10];
                if (crypter != null) {
                    cryptSeq.Add(crypter);
                }
                blockIV /= 10;
            }

            return cryptSeq;
        }

        private Packet Encrypt(byte[] packet, int offset, int length) {
            foreach (ICrypter crypter in encryptSeq) {
                crypter.Encrypt(packet);
            }

            return WriteHeader(packet);
        }

        private Packet Decrypt(byte[] packet, int offset, int length) {
            var reader = new PacketReader(packet);
            int packetSize = ReadHeader(reader);

            foreach (ICrypter crypter in decryptSeq) {
                crypter.Decrypt(packet, HEADER_SIZE, packetSize);
            }

            return new Packet(packet);
        }

        private ushort EncodeSeqBase() {
            ushort encSeq = (ushort)(version ^ (iv >> 16));
            AdvanceIV();
            return encSeq;
        }

        private ushort DecodeSeqBase(ushort encSeq) {
            ushort decSeq = (ushort)((iv >> 16) ^ encSeq);
            AdvanceIV();
            return decSeq;
        }
    }
}