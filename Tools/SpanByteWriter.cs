using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Maple2.PacketLib.Tools {
    public ref struct SpanByteWriter : IByteWriter {
        private const int DEFAULT_SIZE = 512;

        private Span<byte> span;
        private readonly IBufferWriter<byte>? bufferWriter;
        private readonly ArrayPool<byte>? pool;
        private byte[]? rented;
        private readonly bool growable;

        public int Length { readonly get; private set; }

        public readonly int Remaining => bufferWriter is null ? span.Length - Length : int.MaxValue;

        public readonly ReadOnlySpan<byte> WrittenSpan {
            get {
                if (bufferWriter is null) {
                    return span.Slice(0, Length);
                }
                if (bufferWriter is ArrayBufferWriter<byte> arrayWriter) {
                    return arrayWriter.WrittenSpan;
                }
                throw new InvalidOperationException("WrittenSpan is only available for Span, ArrayPool, or ArrayBufferWriter-backed SpanByteWriters.");
            }
        }

        public SpanByteWriter() : this(DEFAULT_SIZE) { }

        public SpanByteWriter(int size) {
            if (size < 0) {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            span = size == 0 ? Span<byte>.Empty : new byte[size];
            bufferWriter = null;
            pool = null;
            rented = null;
            growable = true;
            Length = 0;
        }

        public SpanByteWriter(Span<byte> buffer, int offset = 0) {
            if ((uint)offset > (uint)buffer.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            span = buffer;
            bufferWriter = null;
            pool = null;
            rented = null;
            growable = false;
            Length = offset;
        }

        public SpanByteWriter(Span<byte> initialBuffer, ArrayPool<byte> pool) {
            span = initialBuffer;
            bufferWriter = null;
            this.pool = pool ?? throw new ArgumentNullException(nameof(pool));
            rented = null;
            growable = true;
            Length = 0;
        }

        public SpanByteWriter(IBufferWriter<byte> bufferWriter) {
            span = default;
            this.bufferWriter = bufferWriter ?? throw new ArgumentNullException(nameof(bufferWriter));
            pool = null;
            rented = null;
            growable = false;
            Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<byte> AcquireSpan(int sizeHint) {
            if (bufferWriter is not null) {
                return bufferWriter.GetSpan(sizeHint);
            }

            if ((uint)sizeHint > (uint)(span.Length - Length)) {
                GrowOrThrow(sizeHint);
            }

            return span.Slice(Length, sizeHint);
        }

        private void GrowOrThrow(int sizeHint) {
            if (!growable || sizeHint < 0) {
                throw new IndexOutOfRangeException("SpanByteWriter capacity exceeded.");
            }

            int required = checked(Length + sizeHint);
            int newSize = Math.Max(span.Length * 2, DEFAULT_SIZE);
            while (newSize < required) {
                newSize = checked(newSize * 2);
            }

            if (pool is not null) {
                byte[] copy = pool.Rent(newSize);
                span.Slice(0, Length).CopyTo(copy);
                if (rented is not null) {
                    pool.Return(rented);
                }
                rented = copy;
                span = copy;
            } else {
                byte[] copy = new byte[newSize];
                span.Slice(0, Length).CopyTo(copy);
                span = copy;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Advance(int count) {
            bufferWriter?.Advance(count);
            Length += count;
        }

        public void Seek(int position) {
            if (bufferWriter is not null) {
                throw new NotSupportedException("Cannot seek when SpanByteWriter is backed by an IBufferWriter<byte>.");
            }
            if (position < 0 || position > span.Length) {
                return;
            }

            Length = position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(scoped in T value) where T : struct {
            int size = Unsafe.SizeOf<T>();
            Span<byte> dest = AcquireSpan(size);
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(dest), value);
            Advance(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte[] value) {
            WriteBytes(value.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte[] value, int offset, int length) {
            WriteBytes(value.AsSpan(offset, length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(scoped ReadOnlySpan<byte> value) {
            if (value.IsEmpty) {
                return;
            }

            Span<byte> dest = AcquireSpan(value.Length);
            value.CopyTo(dest);
            Advance(value.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBool(bool value) {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteByte(byte value = 0) {
            Span<byte> dest = AcquireSpan(1);
            dest[0] = value;
            Advance(1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteShort(short value = 0) {
            Span<byte> dest = AcquireSpan(2);
            BinaryPrimitives.WriteInt16LittleEndian(dest, value);
            Advance(2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteInt(int value = 0) {
            Span<byte> dest = AcquireSpan(4);
            BinaryPrimitives.WriteInt32LittleEndian(dest, value);
            Advance(4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteFloat(float value = 0f) {
            Write(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteLong(long value = 0) {
            Span<byte> dest = AcquireSpan(8);
            BinaryPrimitives.WriteInt64LittleEndian(dest, value);
            Advance(8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(string? value = "") {
            WriteString(value.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteString(scoped ReadOnlySpan<char> value) {
            Write<ushort>((ushort)value.Length);
            WriteRawString(value);
        }

        public void WriteRawString(string? value) {
            WriteRawString(value.AsSpan());
        }

        public void WriteRawString(scoped ReadOnlySpan<char> value) {
            int length = value.Length;
            if (length == 0) {
                return;
            }

            Span<byte> dest = AcquireSpan(length);
            for (int i = 0; i < length; i++) {
                dest[i] = (byte)value[i];
            }
            Advance(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUnicodeString(string? value = "") {
            WriteUnicodeString(value.AsSpan());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteUnicodeString(scoped ReadOnlySpan<char> value) {
            Write<ushort>((ushort)value.Length);
            WriteRawUnicodeString(value);
        }

        public void WriteRawUnicodeString(string? value) {
            WriteRawUnicodeString(value.AsSpan());
        }

        public void WriteRawUnicodeString(scoped ReadOnlySpan<char> value) {
            if (value.IsEmpty) {
                return;
            }

            int byteCount = value.Length * 2;
            Span<byte> dest = AcquireSpan(byteCount);
            if (BitConverter.IsLittleEndian) {
                MemoryMarshal.AsBytes(value).CopyTo(dest);
            } else {
                Encoding.Unicode.GetBytes(value, dest);
            }
            Advance(byteCount);
        }

        public readonly byte[] ToArray() {
            return WrittenSpan.ToArray();
        }

        public readonly override string ToString() {
            return WrittenSpan.ToHexString(' ');
        }

        public void Dispose() {
            if (rented is not null && pool is not null) {
                pool.Return(rented);
                rented = null;
            }
        }
    }
}
