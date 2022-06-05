using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Maple2.PacketLib.Tools {
    // ByteWriter backed by ArrayPool. Must Dispose.
    public sealed unsafe class PoolByteWriter : ByteWriter {
        private readonly ArrayPool<byte> pool;
        private bool disposed;

        public PoolByteWriter(int size = DEFAULT_SIZE, ArrayPool<byte>? pool = null)
                : base((pool ?? ArrayPool<byte>.Shared).Rent(size)) {
            this.pool = pool ?? ArrayPool<byte>.Shared;
            Length = 0;
        }

        ~PoolByteWriter() => Dispose(false);

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
            Dispose(true);
            return writer;
        }

        public override void Dispose(bool disposing) {
            if (disposed) {
                return;
            }

            disposed = true;
            pool.Return(Buffer);
        }
    }
}
