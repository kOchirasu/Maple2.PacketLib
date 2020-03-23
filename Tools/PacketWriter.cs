﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MaplePacketLib2.Tools {
    public class PacketWriter : Packet {
        private const int DEFAULT_SIZE = 64;

        public PacketWriter(int size = DEFAULT_SIZE) : base(new byte[size]) {
            this.Length = 0;
        }

        public static PacketWriter Of(ushort opcode, int size = DEFAULT_SIZE) {
            var packet = new PacketWriter(size);
            packet.WriteUShort(opcode);
            return packet;
        }

        private void EnsureCapacity(int length) {
            if (Length + length <= Buffer.Length) {
                return;
            }
            int newSize = Buffer.Length * 2;
            while (newSize < Length + length) {
                newSize *= 2;
            }
            byte[] newBuffer = new byte[newSize];
            System.Buffer.BlockCopy(Buffer, 0, newBuffer, 0, Length);
            Buffer = newBuffer;
        }

        public unsafe void Write<T>(T value) where T : struct {
            int size = Marshal.SizeOf(typeof(T));
            EnsureCapacity(size);
            fixed (byte* ptr = &Buffer[Length]) {
                Marshal.StructureToPtr(value, (IntPtr)ptr, false);
                Length += size;
            }
        }

        public void Write(params byte[] value) {
            EnsureCapacity(value.Length);
            System.Buffer.BlockCopy(value, 0, Buffer, Length, value.Length);
            Length += value.Length;
        }

        public void WriteBool(bool value) {
            WriteByte(value ? (byte)1 : (byte)0);
        }

        public unsafe void WriteByte(byte value = 0) {
            EnsureCapacity(1);
            fixed (byte* ptr = Buffer) {
                *(ptr + Length) = value;
                ++Length;
            }
        }

        public unsafe void WriteShort(short value = 0) {
            EnsureCapacity(2);
            fixed (byte* ptr = Buffer) {
                *(short*)(ptr + Length) = value;
                Length += 2;
            }
        }

        public unsafe void WriteUShort(ushort value = 0) {
            EnsureCapacity(2);
            fixed (byte* ptr = Buffer) {
                *(ushort*)(ptr + Length) = value;
                Length += 2;
            }
        }

        public unsafe void WriteInt(int value = 0) {
            EnsureCapacity(4);
            fixed (byte* ptr = Buffer) {
                *(int*)(ptr + Length) = value;
                Length += 4;
            }
        }

        public unsafe void WriteUInt(uint value = 0) {
            EnsureCapacity(4);
            fixed (byte* ptr = Buffer) {
                *(uint*)(ptr + Length) = value;
                Length += 4;
            }
        }

        public unsafe void WriteLong(long value = 0) {
            EnsureCapacity(8);
            fixed (byte* ptr = Buffer) {
                *(long*)(ptr + Length) = value;
                Length += 8;
            }
        }

        public void Timestamp() {
            Write(Environment.TickCount);
        }

        public void WriteString(string value) {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            Write(bytes);
        }

        public void WritePaddedString(string value, int length, char pad = '\0') {
            WriteString(value);
            for (int i = value.Length; i < length; i++) {
                WriteByte((byte)pad);
            }
        }

        public void WriteUnicodeString(string value) {
            Write((ushort)value.Length);
            byte[] bytes = Encoding.Unicode.GetBytes(value);
            Write(bytes);
        }

        public void WriteMapleString(string value) {
            Write((ushort)value.Length);
            WriteString(value);
        }

        public void WriteHexString(string value) {
            byte[] bytes = value.ToByteArray();
            Write(bytes);
        }

        public void WriteZero(int count) {
            Write(new byte[count]);
        }
    }
}
