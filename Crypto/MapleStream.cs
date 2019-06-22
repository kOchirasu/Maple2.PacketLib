using System;

namespace MaplePacketLib2.Crypto {
    public class MapleStream {
        private const int DEFAULT_SIZE = 4096;
        private const int HEADER_SIZE = 6;

        private byte[] buffer = new byte[DEFAULT_SIZE];
        private int cursor;

        public readonly Func<byte[], byte[]> Encrypt;
        public readonly Func<byte[], byte[]> Decrypt;

        public MapleStream(uint version, uint iv, uint seqBlock) {
            Encrypt = MapleCipher.Encryptor(version, iv, seqBlock).Transform;
            Decrypt = MapleCipher.Decryptor(version, iv, seqBlock).Transform;
        }

        public void Write(byte[] packet) {
            Write(packet, 0, packet.Length);
        }

        public void Write(byte[] packet, int offset, int length) {
            if (buffer.Length - cursor < length) {
                int newSize = buffer.Length * 2;
                while (newSize < cursor + length) {
                    newSize *= 2;
                }
                byte[] newBuffer = new byte[newSize];
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, cursor);
                buffer = newBuffer;
            }
            Buffer.BlockCopy(packet, offset, buffer, cursor, length);
            cursor += length;
        }

        public byte[] Read() {
            if (cursor < HEADER_SIZE) {
                return null;
            }

            int packetSize = BitConverter.ToInt32(buffer, 2);
            int expectedDataSize = packetSize + HEADER_SIZE;
            if (cursor < expectedDataSize) {
                return null;
            }

            byte[] packetBuffer = new byte[HEADER_SIZE + packetSize];
            Buffer.BlockCopy(buffer, 0, packetBuffer, 0, HEADER_SIZE + packetSize);
            byte[] result = Decrypt(packetBuffer);

            cursor -= expectedDataSize;
            Buffer.BlockCopy(buffer, expectedDataSize, buffer, 0, cursor);

            return result;
        }
    }
}
