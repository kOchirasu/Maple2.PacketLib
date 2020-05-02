using System.Buffers;

namespace MaplePacketLib2.Tools {
    // For use with System.IO.Pipelines
    // Read recv packets directly from underlying data stream without needing to buffer
    public static class MaplePipe {
        private const int HEADER_SIZE = 6;

        public static bool TryRead(ReadOnlySequence<byte> buffer, out byte[] packet) {
            if (buffer.Length < HEADER_SIZE) {
                packet = null;
                return false;
            }

            SequenceReader<byte> reader = new SequenceReader<byte>(buffer.Slice(2, 4));
            reader.TryReadLittleEndian(out int packetSize);
            int bufferSize = HEADER_SIZE + packetSize;
            if (buffer.Length < bufferSize) {
                packet = null;
                return false;
            }

            packet = buffer.Slice(0, bufferSize).ToArray();
            return true;
        }
    }
}