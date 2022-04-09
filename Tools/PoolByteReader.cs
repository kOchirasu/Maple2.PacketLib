using System;
using System.Buffers;

namespace Maple2.PacketLib.Tools {
    // ByteReader backed by ArrayPool. Must Dispose.
    public sealed class PoolByteReader : ByteReader, IDisposable {
        private readonly ArrayPool<byte> pool;
        private bool disposed;

        public PoolByteReader(ArrayPool<byte> pool, byte[] packet, int length, int offset = 0) : base(packet, offset) {
            this.pool = pool;
            // ArrayPool can return an array of greater length than needed.
            this.Length = length;
        }

        ~PoolByteReader() => Dispose(false);

        public new void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing) {
            if (disposed) {
                return;
            }

            pool.Return(Buffer);
            disposed = true;
        }
    }
}
