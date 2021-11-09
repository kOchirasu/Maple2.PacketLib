using System;
using System.Buffers;

namespace Maple2.PacketLib.Tools {
    // ByteReader backed by ArrayPool. Must Dispose.
    public class PoolByteReader : ByteReader, IDisposable {
        private readonly ArrayPool<byte> pool;

        public PoolByteReader(ArrayPool<byte> pool, byte[] packet, int length, int offset = 0) : base(packet, offset) {
            this.pool = pool;
            // ArrayPool can return an array of greater length than needed.
            this.Length = length;
        }

        public new void Dispose() {
            pool.Return(Buffer);
#if DEBUG
            // In DEBUG, SuppressFinalize to mark object as disposed.
            GC.SuppressFinalize(this);
#endif
        }

#if DEBUG
        // Provides warning if Disposed in not called.
        ~PoolByteReader() {
            System.Diagnostics.Debug.Fail($"PacketReader not disposed: {this}");
        }
#endif
    }
}
