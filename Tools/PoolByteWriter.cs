using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Maple2.PacketLib.Tools {
    // ByteWriter backed by ArrayPool. Must Dispose.
    public unsafe class PoolByteWriter : ByteWriter, IDisposable {
        private readonly ArrayPool<byte> pool;
        private bool disposed;

        public PoolByteWriter(int size = DEFAULT_SIZE, ArrayPool<byte> pool = null)
                : base((pool ?? ArrayPool<byte>.Shared).Rent(size)) {
            this.pool = pool ?? ArrayPool<byte>.Shared;
            Length = 0;
        }

        public override void ResizeBuffer(int newSize) {
            if (newSize < Buffer.Length) {
                throw new ArgumentException("Cannot decrease buffer size.");
            }

            byte[] copy = pool.Rent(newSize);
            fixed (byte* ptr = Buffer)
            fixed (byte* copyPtr = copy) {
                Unsafe.CopyBlock(copyPtr, ptr, (uint) Length);
            }
            pool.Return(Buffer);

            Buffer = copy;
        }

        // Returns a managed array ByteWriter and disposes this instance.
        public ByteWriter Managed() {
            var writer = new ByteWriter(ToArray(), Length);
            Dispose();
            return writer;
        }

        public new void Dispose() {
            if (!disposed) {
                disposed = true;
                pool.Return(Buffer);
            }
#if DEBUG
            // In DEBUG, SuppressFinalize to mark object as disposed.
            GC.SuppressFinalize(this);
#endif
        }

#if DEBUG
        // Provides warning if Disposed in not called.
        ~PoolByteWriter() {
            System.Diagnostics.Debug.Fail($"PacketWriter not disposed: {this}");
        }
#endif
    }
}
