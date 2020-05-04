using System.Runtime.CompilerServices;

namespace MaplePacketLib2.Tools {
    public class Packet {
        public byte[] Buffer { get; protected set; }

        public int Length { get; protected set; }

        public Packet(byte[] buffer) {
            this.Buffer = buffer;
            this.Length = buffer.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketReader Reader() {
            return new PacketReader(Buffer) {Length = Length};
        }

        public unsafe byte[] ToArray() {
            byte[] copy = new byte[Length];
            fixed (byte* ptr = Buffer)
            fixed (byte* copyPtr = copy) {
                Unsafe.CopyBlock(copyPtr, ptr, (uint) Length);
            }

            return copy;
        }

        public override string ToString() {
            return Buffer.ToHexString(Length, ' ');
        }
    }
}