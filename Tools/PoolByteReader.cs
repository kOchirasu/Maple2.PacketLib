using System;
using System.Buffers;

namespace MaplePacketLib2.Tools {
    // ByteReader backed by ArrayPool. Must Dispose.
    public class PoolByteReader : ByteReader, IDisposable {
        private readonly ArrayPool<byte> pool;

        public PoolByteReader(ArrayPool<byte> pool, byte[] packet, int offset = 0) : base(packet, offset) {
            this.pool = pool;
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
