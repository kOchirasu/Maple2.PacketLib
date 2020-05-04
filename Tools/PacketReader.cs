using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace MaplePacketLib2.Tools {
    public unsafe class PacketReader : Packet {
        public int Position { get; private set; }

        public int Available => Length - Position;

        public PacketReader(byte[] packet, int offset = 0) : base(packet) {
            this.Position = offset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckLength(int length) {
            int index = Position + length;
            if (index > Length || index < Position) {
                throw new IndexOutOfRangeException($"Not enough space in packet: {this}\n");
            }
        }

        public T Read<T>() where T : struct {
            int size = Unsafe.SizeOf<T>();
            CheckLength(size);
            fixed (byte* ptr = &Buffer[Position]) {
                var value = Unsafe.Read<T>(ptr);
                Position += size;
                return value;
            }
        }

        public byte[] ReadBytes(int count) {
            CheckLength(count);
            byte[] bytes = new byte[count];
            fixed (byte* ptr = &Buffer[Position])
            fixed (byte* bytesPtr = bytes){
                Unsafe.CopyBlock(bytesPtr, ptr, (uint)count);
            }

            Position += count;
            return bytes;
        }

        public byte ReadByte() {
            CheckLength(1);
            return Buffer[Position++];
        }

        public bool ReadBool() {
            return ReadByte() != 0;
        }

        public short ReadShort() {
            CheckLength(2);
            fixed (byte* ptr = &Buffer[Position]) {
                short value = *(short*)ptr;
                Position += 2;
                return value;
            }
        }

        public ushort ReadUShort() {
            CheckLength(2);
            fixed (byte* ptr = &Buffer[Position]) {
                ushort value = *(ushort*)ptr;
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

        public uint ReadUInt() {
            CheckLength(4);
            fixed (byte* ptr = &Buffer[Position]) {
                uint value = *(uint*)ptr;
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
            ushort count = ReadUShort();
            return ReadRawString(count);
        }

        public string ReadRawString(int length) {
            CheckLength(length);
            fixed (byte* ptr = &Buffer[Position]) {
                string value = new string((sbyte*) ptr, 0, length, Encoding.UTF8);
                Position += length;
                return value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReadUnicodeString() {
            ushort length = ReadUShort();
            return ReadRawUnicodeString(length);
        }

        public string ReadRawUnicodeString(int length) {
            CheckLength(length * 2);
            fixed (byte* ptr = &Buffer[Position]) {
                string value = new string((sbyte*) ptr, 0, length * 2, Encoding.Unicode);
                Position += length * 2;
                return value;
            }
        }

        public string ReadHexString(int length) {
            return ReadBytes(length).ToHexString(' ');
        }

        public void Skip(int count) {
            int index = Position + count;
            if (index > Length || index < 0) { // Allow backwards seeking
                throw new IndexOutOfRangeException($"Not enough space in packet: {this}\n");
            }
            Position += count;
        }
    }
}
