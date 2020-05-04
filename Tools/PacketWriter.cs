using System;
using System.Runtime.CompilerServices;

namespace MaplePacketLib2.Tools {
    public unsafe class PacketWriter : Packet {
        private const int DEFAULT_SIZE = 512;

        public int Remaining => Buffer.Length - Length;

        public PacketWriter(int size = DEFAULT_SIZE) : base(new byte[size]) {
            this.Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PacketWriter Of(ushort opcode, int size = DEFAULT_SIZE) {
            var packet = new PacketWriter(size);
            packet.WriteUShort(opcode);
            return packet;
        }

        private void EnsureCapacity(int length) {
            int required = Length + length;
            if (Buffer.Length >= required) {
                return;
            }

            int newSize = Buffer.Length * 2;
            while (newSize < required) {
                newSize *= 2;
            }

            ResizeBuffer(newSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ResizeBuffer(int newSize) {
            if (newSize < Buffer.Length) {
                throw new ArgumentException("Cannot decrease buffer size.");
            }

            byte[] copy = new byte[newSize];
            fixed (byte* ptr = Buffer)
            fixed (byte* copyPtr = copy) {
                Unsafe.CopyBlock(copyPtr, ptr, (uint) Length);
            }

            Buffer = copy;
        }

        public void Seek(int position) {
            if (position < 0 || position > Buffer.Length) {
                return;
            }

            Length = position;
        }

        public PacketWriter Write<T>(in T value) where T : struct {
            int size = Unsafe.SizeOf<T>();
            EnsureCapacity(size);
            fixed (byte* ptr = &Buffer[Length]) {
                Unsafe.Write<T>(ptr, value);
                Length += size;
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketWriter WriteBytes(byte[] value) {
            return WriteBytes(value, 0, value.Length);
        }

        public PacketWriter WriteBytes(byte[] value, int offset, int length) {
            EnsureCapacity(length);
            fixed (byte* ptr = &Buffer[Length])
            fixed (byte* valuePtr = value) {
                Unsafe.CopyBlock(ptr, valuePtr + offset, (uint)length);
                Length += length;
            }

            return this;
        }

        public PacketWriter WriteBool(bool value) {
            EnsureCapacity(1);
            Buffer[Length++] = value ? (byte)1 : (byte)0;

            return this;
        }

        public PacketWriter WriteByte(byte value = 0) {
            EnsureCapacity(1);
            Buffer[Length++] = value;

            return this;
        }

        public PacketWriter WriteShort(short value = 0) {
            EnsureCapacity(2);
            fixed (byte* ptr = &Buffer[Length]) {
                *(short*)ptr = value;
                Length += 2;
            }

            return this;
        }

        public PacketWriter WriteUShort(ushort value = 0) {
            EnsureCapacity(2);
            fixed (byte* ptr = &Buffer[Length]) {
                *(ushort*)ptr = value;
                Length += 2;
            }

            return this;
        }

        public PacketWriter WriteInt(int value = 0) {
            EnsureCapacity(4);
            fixed (byte* ptr = &Buffer[Length]) {
                *(int*)ptr = value;
                Length += 4;
            }

            return this;
        }

        public PacketWriter WriteUInt(uint value = 0) {
            EnsureCapacity(4);
            fixed (byte* ptr = &Buffer[Length]) {
                *(uint*)ptr = value;
                Length += 4;
            }

            return this;
        }

        public PacketWriter WriteLong(long value = 0) {
            EnsureCapacity(8);
            fixed (byte* ptr = &Buffer[Length]) {
                *(long*)ptr = value;
                Length += 8;
            }

            return this;
        }

        public PacketWriter WriteULong(ulong value = 0) {
            EnsureCapacity(8);
            fixed (byte* ptr = &Buffer[Length]) {
                *(ulong*)ptr = value;
                Length += 8;
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketWriter WriteString(string value) {
            WriteUShort((ushort)value.Length);
            return WriteRawString(value);
        }

        // Note: char and string are UTF-16 in C#
        public PacketWriter WriteRawString(string value) {
            int length = value.Length;
            EnsureCapacity(length);
            fixed (char* valuePtr = value) {
                for (int i = 0; i < length; i++) {
                    Buffer[Length++] = (byte) *(valuePtr + i);
                }
            }

            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PacketWriter WriteUnicodeString(string value) {
            WriteUShort((ushort)value.Length);
            return WriteRawUnicodeString(value);
        }

        public PacketWriter WriteRawUnicodeString(string value) {
            int length = value.Length * 2;
            EnsureCapacity(length);
            fixed (byte* ptr = &Buffer[Length])
            fixed (char* valuePtr = value) {
                Unsafe.CopyBlock(ptr, valuePtr, (uint)length);
                Length += length;
            }

            return this;
        }

        public PacketWriter WriteHexString(string value) {
            byte[] bytes = value.ToByteArray();
            return WriteBytes(bytes);
        }

        public PacketWriter WriteZero(int count) {
            EnsureCapacity(count);
            Length += count;
            return this;
        }
    }
}
