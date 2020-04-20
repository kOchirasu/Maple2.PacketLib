using System;
using System.IO;
using MaplePacketLib2.Crypto;

namespace MaplePacketLib2.Tools {
    // Converts a stream of bytes into individual packets
    public class MapleStream : Stream {
        private const int DEFAULT_SIZE = 4096;
        private const int HEADER_SIZE = 6;

        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => stream.CanWrite;
        public override long Length => stream.Length;
        public override long Position {
            get => stream.Position;
            set => stream.Position = value;
        }

        private readonly Stream stream;
        private readonly MapleCipher writeCipher;
        private readonly MapleCipher readCipher;
        private byte[] buffer = new byte[DEFAULT_SIZE];
        private int cryptoCursor; // End of decrypted data in buffer
        private int cursor; // End of data in buffer

        public MapleStream(Stream stream, MapleCipher writeCipher, MapleCipher readCipher) {
            this.stream = stream;
            this.writeCipher = writeCipher;
            this.readCipher = readCipher;
        }

        public override void Flush() {
            stream.Flush();
        }

        public void Write(byte[] packet) {
            Write(packet, 0, packet.Length);
        }

        public override void Write(byte[] packet, int offset, int count) {
            writeCipher.Transform(packet, offset, count);

            stream.Write(packet, offset, count);
        }

        public override int Read(byte[] packet, int offset, int count) {
            int baseCursor = 0; // Start of data in buffer
            int remainingBytes = count;
            int packetOffset = offset;
            if (cryptoCursor != 0) {
                if (cryptoCursor <= count) {
                    Buffer.BlockCopy(buffer, 0, packet, offset, cryptoCursor);
                    remainingBytes -= cryptoCursor;
                    packetOffset += cryptoCursor;
                    baseCursor += cryptoCursor;
                } else {
                    Buffer.BlockCopy(buffer, 0, packet, offset, count);
                    cursor -= count;
                    Buffer.BlockCopy(buffer, count, buffer, 0, cursor);
                    cryptoCursor -= count;

                    return count;
                }
            }

            // Double buffer size if no space remaining
            int remaining = buffer.Length - cursor;
            if (remaining == 0) {
                int newSize = buffer.Length * 2;
                byte[] newBuffer = new byte[newSize];
                Buffer.BlockCopy(buffer, 0, newBuffer, 0, cursor);
                buffer = newBuffer;
                remaining = buffer.Length - cursor;
            }

            cursor += stream.Read(buffer, cursor, remaining);

            int length = cursor - cryptoCursor;
            if (length < HEADER_SIZE) {
                cursor -= baseCursor;
                Buffer.BlockCopy(buffer, baseCursor, buffer, 0, cursor);
                cryptoCursor = 0;

                return 0;
            }

            int packetDataSize = BitConverter.ToInt32(buffer, cryptoCursor + 2);
            int packetSize = HEADER_SIZE + packetDataSize;
            if (length < packetSize) {
                cursor -= baseCursor;
                Buffer.BlockCopy(buffer, baseCursor, buffer, 0, cursor);
                cryptoCursor = 0;

                return 0;
            }

            // If we get here, we know there is more data to add to output packet
            readCipher.Transform(buffer, cryptoCursor, packetSize);
            cryptoCursor += packetSize;

            int bytesToOutput = Math.Min(remainingBytes, cryptoCursor);
            Buffer.BlockCopy(buffer, baseCursor, packet, packetOffset, bytesToOutput);
            packetOffset += bytesToOutput;
            baseCursor += bytesToOutput;

            cursor -= baseCursor;
            Buffer.BlockCopy(buffer, baseCursor, buffer, 0, cursor);
            cryptoCursor -= bytesToOutput;

            return packetOffset - offset;
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("MapleStream does not support Seek.");
        }

        public override void SetLength(long value) {
            throw new NotSupportedException("MapleStream does not support SetLength.");
        }
    }
}
