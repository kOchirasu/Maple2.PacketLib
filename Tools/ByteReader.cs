using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Maple2.PacketLib.Tools {
    public unsafe class ByteReader : IByteReader {
        public byte[] Buffer { get; }
        public int Length { get; protected set; }
        public int Position { get; protected set; }

        public int Available => Length - Position;

        public ByteReader(byte[] packet, int offset = 0) {
            Buffer = packet;
            Length = packet.Length;
            Position = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public SpanByteReader AsSpanReader() {
            return new SpanByteReader(Buffer.AsSpan(Position, Available));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CheckLength(int length) {
            if ((uint)length > (uint)(Length - Position)) {
                throw new IndexOutOfRangeException($"Not enough space in packet: {this}\n");
            }
        }

        public T Read<T>() where T : struct {
            int size = Unsafe.SizeOf<T>();
            ReadOnlySpan<byte> slice = ReadSpan(size);
            return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(slice));
        }

        public T Peek<T>() where T : struct {
            int size = Unsafe.SizeOf<T>();
            CheckLength(size);
            return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(Buffer.AsSpan(Position, size)));
        }

        public ReadOnlySpan<byte> ReadSpan(int count) {
            CheckLength(count);
            ReadOnlySpan<byte> slice = Buffer.AsSpan(Position, count);
            Position += count;
            return slice;
        }

        public void ReadBytes(scoped Span<byte> destination) {
            ReadSpan(destination.Length).CopyTo(destination);
        }

        public byte[] ReadBytes(int count) {
            if (count == 0) {
                return Array.Empty<byte>();
            }

            return ReadSpan(count).ToArray();
        }

        public bool ReadBool() {
            return ReadByte() != 0;
        }

        public byte ReadByte() {
            CheckLength(1);
            return Buffer[Position++];
        }

        public short ReadShort() {
            CheckLength(2);
            fixed (byte* ptr = &Buffer[Position]) {
                short value = *(short*)ptr;
                Position += 2;
                return value;
            }
        }

        public int ReadInt() {
            CheckLength(4);
            fixed (byte* ptr = &Buffer[Position]) {
                int value = *(int*)ptr;
                Position += 4;
                return value;
            }
        }

        public float ReadFloat() {
            CheckLength(4);
            fixed (byte* ptr = &Buffer[Position]) {
                float value = *(float*)ptr;
                Position += 4;
                return value;
            }
        }

        public long ReadLong() {
            CheckLength(8);
            fixed (byte* ptr = &Buffer[Position]) {
                long value = *(long*)ptr;
                Position += 8;
                return value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString() {
            ushort length = Read<ushort>();
            return ReadRawString(length);
        }

        public string ReadRawString(int length) {
            if (length == 0) {
                return string.Empty;
            }

            CheckLength(length);
            string value = new string('\0', length);
            fixed (byte* ptr = &Buffer[Position])
            fixed (char* dst = value) {
                for (int i = 0; i < length; i++) {
                    dst[i] = (char)ptr[i];
                }
            }
            Position += length;
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUnicodeString() {
            ushort length = Read<ushort>();
            return ReadRawUnicodeString(length);
        }

        public string ReadRawUnicodeString(int length) {
            if (length == 0) {
                return string.Empty;
            }

            if ((uint)length > (uint)((Length - Position) >> 1)) {
                throw new IndexOutOfRangeException($"Not enough space in packet: {this}\n");
            }

            int byteCount = length * 2;
            fixed (byte* ptr = &Buffer[Position]) {
                string value = new string((sbyte*)ptr, 0, byteCount, Encoding.Unicode);
                Position += byteCount;
                return value;
            }
        }

        public void Skip(int count) {
            int index = Position + count;
            if ((uint)index > (uint)Length) { // Allow backwards seeking
                throw new IndexOutOfRangeException($"Not enough space in packet: {this}\n");
            }
            Position = index;
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
