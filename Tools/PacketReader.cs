﻿using System;
using System.Runtime.InteropServices;
using System.Text;

namespace MaplePacketLib2.Tools {
    public class PacketReader {
        public byte[] Buffer { get; }
        public int Position { get; private set; }

        public int Available => Buffer.Length - Position;

        public PacketReader(byte[] packet, int skip = 0) {
            Buffer = packet;
            Position = skip;
        }

        private void CheckLength(int length) {
            int index = Position + length;
            if (index > Buffer.Length || index < Position) {
                throw new IndexOutOfRangeException($"Not enough space in packet: {ToString()}\n");
            }
        }

        public unsafe T Read<T>() where T : struct {
            int size = Marshal.SizeOf(typeof(T));
            CheckLength(size);
            fixed (byte* ptr = &Buffer[Position]) {
                T value = (T)Marshal.PtrToStructure((IntPtr)ptr, typeof(T));
                Position += size;
                return value;
            }
        }

        public byte[] Read(int count) {
            CheckLength(count);
            byte[] bytes = new byte[count];
            System.Buffer.BlockCopy(Buffer, Position, bytes, 0, count);
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

        public unsafe short ReadShort() {
            CheckLength(2);
            fixed (byte* ptr = Buffer) {
                short value = *(short*)(ptr + Position);
                Position += 2;
                return value;
            }
        }

        public unsafe ushort ReadUShort() {
            CheckLength(2);
            fixed (byte* ptr = Buffer) {
                ushort value = *(ushort*)(ptr + Position);
                Position += 2;
                return value;
            }
        }

        public unsafe int ReadInt() {
            CheckLength(4);
            fixed (byte* ptr = Buffer) {
                int value = *(int*)(ptr + Position);
                Position += 4;
                return value;
            }
        }

        public unsafe uint ReadUInt() {
            CheckLength(4);
            fixed (byte* ptr = Buffer) {
                uint value = *(uint*)(ptr + Position);
                Position += 4;
                return value;
            }
        }

        public unsafe long ReadLong() {
            CheckLength(8);
            fixed (byte* ptr = Buffer) {
                long value = *(long*)(ptr + Position);
                Position += 8;
                return value;
            }
        }

        public string ReadString(int count) {
            byte[] bytes = Read(count);
            return Encoding.UTF8.GetString(bytes);
        }

        public string ReadUnicodeString() {
            ushort count = Read<ushort>();
            byte[] bytes = Read(count * 2);
            return Encoding.Unicode.GetString(bytes);
        }

        public string ReadMapleString() {
            ushort count = Read<ushort>();
            return ReadString(count);
        }

        public string ReadHexString(int count) {
            return Read(count).ToHexString(' ');
        }

        public void Skip(int count) {
            int index = Position + count;
            if (index > Buffer.Length || index < 0) { // Allow backwards seeking
                throw new IndexOutOfRangeException($"Not enough space in packet: {ToString()}\n");
            }
            Position += count;
        }

        public void Next(byte b) {
            int pos = Array.IndexOf(Buffer, b, Position);
            Skip(pos - Position + 1);
        }

        public byte[] ToArray() {
            byte[] copy = new byte[Buffer.Length];
            System.Buffer.BlockCopy(Buffer, 0, copy, 0, Buffer.Length);
            return copy;
        }

        public override string ToString() {
            return Buffer.ToHexString(' ');
        }
    }
}
