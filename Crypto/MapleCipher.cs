using MaplePacketLib2.Tools;
using System;
using System.Buffers;
using System.Collections.Generic;

namespace MaplePacketLib2.Crypto {
    public class MapleCipher {
        private const int HEADER_SIZE = 6;

        private readonly uint version;
        private uint iv;

        private MapleCipher(uint version, uint iv) {
            this.version = version;
            this.iv = iv;
        }

        private void AdvanceIV() {
            iv = Rand32.CrtRand(iv);
        }

        private static List<ICrypter> InitCryptSeq(uint version, uint blockIV) {
            ICrypter[] crypt = new ICrypter[4];
            crypt[RearrangeCrypter.GetIndex(version)] = new RearrangeCrypter();
            crypt[XORCrypter.GetIndex(version)] = new XORCrypter(version);
            crypt[TableCrypter.GetIndex(version)] = new TableCrypter(version);

            List<ICrypter> cryptSeq = new List<ICrypter>();
            while (blockIV > 0) {
                ICrypter crypter = crypt[blockIV % 10];
                if (crypter != null) {
                    cryptSeq.Add(crypter);
                }
                blockIV /= 10;
            }

            return cryptSeq;
        }

        // These classes are used for encryption/decryption
        public class Encryptor {
            private readonly MapleCipher cipher;
            private readonly ICrypter[] encryptSeq;

            public Encryptor(uint version, uint iv, uint blockIV) {
                this.cipher = new MapleCipher(version, iv);
                this.encryptSeq = InitCryptSeq(version, blockIV).ToArray();
            }

            public Packet WriteHeader(byte[] packet, int offset, int length) {
                short encSeq = EncodeSeqBase();

                var writer = new PacketWriter(length + HEADER_SIZE);
                writer.WriteShort(encSeq);
                writer.WriteInt(length);
                writer.WriteBytes(packet, offset, length);

                return writer;
            }

            private short EncodeSeqBase() {
                short encSeq = (short)(cipher.version ^ (cipher.iv >> 16));
                cipher.AdvanceIV();
                return encSeq;
            }

            public Packet Encrypt(Packet packet) {
                return Encrypt(packet.Buffer, 0, packet.Length);
            }

            public Packet Encrypt(byte[] packet, int offset, int length) {
                Packet result = WriteHeader(packet, offset, length);
                foreach (ICrypter crypter in encryptSeq) {
                    crypter.Encrypt(result.Buffer, HEADER_SIZE, HEADER_SIZE + length);
                }

                return result;
            }
        }

        public class Decryptor {
            private readonly MapleCipher cipher;
            private readonly ICrypter[] decryptSeq;

            public Decryptor(uint version, uint iv, uint blockIV) {
                this.cipher = new MapleCipher(version, iv);
                List<ICrypter> cryptSeq = InitCryptSeq(version, blockIV);
                cryptSeq.Reverse();
                decryptSeq = cryptSeq.ToArray();
            }

            private short DecodeSeqBase(short encSeq) {
                short decSeq = (short)((cipher.iv >> 16) ^ encSeq);
                cipher.AdvanceIV();
                return decSeq;
            }

            // For use with System.IO.Pipelines
            // Decrypt packets directly from underlying data stream without needing to buffer
            public int TryDecrypt(ReadOnlySequence<byte> buffer, out Packet packet) {
                if (buffer.Length < HEADER_SIZE) {
                    packet = null;
                    return 0;
                }

                SequenceReader<byte> reader = new SequenceReader<byte>(buffer);
                reader.TryReadLittleEndian(out short encSeq);
                reader.TryReadLittleEndian(out int packetSize);
                int rawPacketSize = HEADER_SIZE + packetSize;
                if (buffer.Length < rawPacketSize) {
                    packet = null;
                    return 0;
                }

                // Only decode sequence once we know there is sufficient data because it mutates IV
                short decSeq = DecodeSeqBase(encSeq);
                if (decSeq != cipher.version) {
                    throw new ArgumentException($"Packet has invalid sequence header: {decSeq}");
                }

                byte[] data = buffer.Slice(HEADER_SIZE, packetSize).ToArray();
                foreach (ICrypter crypter in decryptSeq) {
                    crypter.Decrypt(data);
                }

                packet = new Packet(data);
                return rawPacketSize;
            }

            public Packet Decrypt(byte[] rawPacket, int offset = 0) {
                var reader = new PacketReader(rawPacket, offset);

                short encSeq = reader.ReadShort();
                short decSeq = DecodeSeqBase(encSeq);
                if (decSeq != cipher.version) {
                    throw new ArgumentException($"Packet has invalid sequence header: {decSeq}");
                }

                int packetSize = reader.ReadInt();
                if (rawPacket.Length < packetSize + HEADER_SIZE) {
                    throw new ArgumentException($"Packet has invalid length: {rawPacket.Length}");
                }

                byte[] packet = reader.ReadBytes(packetSize);
                foreach (ICrypter crypter in decryptSeq) {
                    crypter.Decrypt(packet);
                }

                return new Packet(packet);
            }
        }
    }
}