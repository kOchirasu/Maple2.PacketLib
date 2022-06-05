using System.Buffers;

namespace Maple2.PacketLib.Tools {
    // ByteReader backed by ArrayPool. Must Dispose.
    public sealed class PoolByteReader : ByteReader {
        private readonly ArrayPool<byte> pool;
        private bool disposed;

        public PoolByteReader(ArrayPool<byte> pool, byte[] packet, int length, int offset = 0) : base(packet, offset) {
            this.pool = pool;
            // ArrayPool can return an array of greater length than needed.
            this.Length = length;
        }

        ~PoolByteReader() => Dispose(false);

        public override void Dispose(bool disposing) {
            if (disposed) {
                return;
            }

            disposed = true;
            pool.Return(Buffer);
        }
    }
}
