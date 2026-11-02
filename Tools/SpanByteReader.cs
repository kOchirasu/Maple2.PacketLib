using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Maple2.PacketLib.Tools {
    public ref struct SpanByteReader : IByteReader {
        private int position;

        public ReadOnlySpan<byte> Span { get; }
        public int Position {
            readonly get => position;
            set {
                if ((uint)value > (uint)Span.Length) {
                    throw new IndexOutOfRangeException($"Not enough space in packet: {ToString()}\n");
                }
                position = value;
            }
        }

        public readonly int Length => Span.Length;
        public readonly int Available => Span.Length - position;
        public readonly ReadOnlySpan<byte> RemainingSpan => Span.Slice(position);

        public SpanByteReader(ReadOnlySpan<byte> packet, int offset = 0) {
            if ((uint)offset > (uint)packet.Length) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            Span = packet;
            position = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckLength(int length) {
            if ((uint)length > (uint)(Span.Length - position)) {
                throw new IndexOutOfRangeException($"Not enough space in packet: {ToString()}\n");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Read<T>() where T : struct {
            int size = Unsafe.SizeOf<T>();
            ReadOnlySpan<byte> slice = ReadSpan(size);
            return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(slice));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly T Peek<T>() where T : struct {
            int size = Unsafe.SizeOf<T>();
            CheckLength(size);
            return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(Span.Slice(position, size)));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> ReadSpan(int count) {
            CheckLength(count);
            ReadOnlySpan<byte> slice = Span.Slice(position, count);
            position += count;
            return slice;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadBytes(scoped Span<byte> destination) {
            ReadSpan(destination.Length).CopyTo(destination);
        }

        public byte[] ReadBytes(int count) {
            if (count == 0) {
                return Array.Empty<byte>();
            }

            return ReadSpan(count).ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBool() {
            return ReadByte() != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte() {
            CheckLength(1);
            return Span[position++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public short ReadShort() {
            return BinaryPrimitives.ReadInt16LittleEndian(ReadSpan(2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ReadInt() {
            return BinaryPrimitives.ReadInt32LittleEndian(ReadSpan(4));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float ReadFloat() {
            return Read<float>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadLong() {
            return BinaryPrimitives.ReadInt64LittleEndian(ReadSpan(8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadString() {
            ushort length = Read<ushort>();
            return ReadRawString(length);
        }

        public unsafe string ReadRawString(int length) {
            if (length == 0) {
                return string.Empty;
            }

            ReadOnlySpan<byte> slice = ReadSpan(length);
            string value = new string('\0', length);
            fixed (char* dst = value) {
                for (int i = 0; i < length; i++) {
                    dst[i] = (char)slice[i];
                }
            }
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

            if ((uint)length > (uint)((Span.Length - position) >> 1)) {
                throw new IndexOutOfRangeException($"Not enough space in packet: {ToString()}\n");
            }

            ReadOnlySpan<byte> slice = ReadSpan(length * 2);
            if (BitConverter.IsLittleEndian) {
                return new string(MemoryMarshal.Cast<byte, char>(slice));
            }

            return Encoding.Unicode.GetString(slice);
        }

        public void Skip(int count) {
            int index = position + count;
            if ((uint)index > (uint)Span.Length) { // Allow backwards seeking
                throw new IndexOutOfRangeException($"Not enough space in packet: {ToString()}\n");
            }
            position = index;
        }

        public readonly override string ToString() {
            return Span.ToHexString(' ');
        }

        public readonly void Dispose() { }
    }
}
