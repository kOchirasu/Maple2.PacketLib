using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Maple2.PacketLib.Tools {
    public unsafe class ByteWriter : IByteWriter {
        protected const int DEFAULT_SIZE = 512;

        public byte[] Buffer { get; protected set; }
        public int Length { get; protected set; }

        public int Remaining => Buffer.Length - Length;

        public ReadOnlySpan<byte> WrittenSpan => Buffer.AsSpan(0, Length);

        public ByteWriter(int size = DEFAULT_SIZE) : this(new byte[size]) { }

        public ByteWriter(byte[] buffer, int offset = 0) {
            Buffer = buffer;
            Length = offset;
        }

        private void EnsureCapacity(int length) {
            int required = Length + length;
            if (Buffer.Length >= required) {
                return;
            }

            int newSize = Math.Max(Buffer.Length * 2, DEFAULT_SIZE);
            while (newSize < required) {
                newSize *= 2;
            }

            ResizeBuffer(newSize);
        }

        public virtual void ResizeBuffer(int newSize) {
            byte[] copy = new byte[newSize];
            Buffer.AsSpan(0, Length).CopyTo(copy);
            Buffer = copy;
        }

        public void Seek(int position) {
            if (position < 0 || position > Buffer.Length) {
                return;
            }

            Length = position;
        }

        public void Write<T>(scoped in T value) where T : struct {
            int size = Unsafe.SizeOf<T>();
            EnsureCapacity(size);
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(Buffer.AsSpan(Length, size)), value);
            Length += size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteBytes(byte[] value) {
            WriteBytes(value.AsSpan());
        }

        public void WriteBytes(byte[] value, int offset, int length) {
            WriteBytes(value.AsSpan(offset, length));
        }

        public void WriteBytes(scoped ReadOnlySpan<byte> value) {
            if (value.IsEmpty) {
                return;
            }

            EnsureCapacity(value.Length);
            value.CopyTo(Buffer.AsSpan(Length, value.Length));
            Length += value.Length;
        }

        public void WriteBool(bool value) {
            EnsureCapacity(1);
            Buffer[Length++] = value ? (byte)1 : (byte)0;
        }

        public void WriteByte(byte value = 0) {
            EnsureCapacity(1);
            Buffer[Length++] = value;
        }

        public void WriteShort(short value = 0) {
            EnsureCapacity(2);
            fixed (byte* ptr = &Buffer[Length]) {
                *(short*)ptr = value;
                Length += 2;
            }
        }

        public void WriteInt(int value = 0) {
            EnsureCapacity(4);
            fixed (byte* ptr = &Buffer[Length]) {
                *(int*)ptr = value;
                Length += 4;
            }
        }

        public void WriteFloat(float value = 0f) {
            EnsureCapacity(4);
            fixed (byte* ptr = &Buffer[Length]) {
                *(float*)ptr = value;
                Length += 4;
            }
        }

        public void WriteLong(long value = 0) {
            EnsureCapacity(8);
            fixed (byte* ptr = &Buffer[Length]) {
                *(long*)ptr = value;
                Length += 8;
            }
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

        // Note: char and string are UTF-16 in C#
        public void WriteRawString(string? value) {
            WriteRawString(value.AsSpan());
        }

        public void WriteRawString(scoped ReadOnlySpan<char> value) {
            int length = value.Length;
            if (length == 0) {
                return;
            }

            EnsureCapacity(length);
            for (int i = 0; i < length; i++) {
                Buffer[Length++] = (byte)value[i];
            }
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
            EnsureCapacity(byteCount);
            if (BitConverter.IsLittleEndian) {
                MemoryMarshal.AsBytes(value).CopyTo(Buffer.AsSpan(Length, byteCount));
            } else {
                Encoding.Unicode.GetBytes(value, Buffer.AsSpan(Length, byteCount));
            }
            Length += byteCount;
        }

        public byte[] ToArray() {
            return Buffer.AsSpan(0, Length).ToArray();
        }

        public override string ToString() {
            return Buffer.ToHexString(Length, ' ');
        }

        public virtual void Dispose(bool disposing) { }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
