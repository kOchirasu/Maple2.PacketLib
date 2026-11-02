using Maple2.PacketLib.Tools;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Maple2.PacketLib.Crypto {
    public class MapleCipher {
        public const int HEADER_SIZE = 6;

        public static int MaxPacketSize { get; set; } = 16 * 1024 * 1024;
        public static ArrayPool<byte> ArrayProvider = ArrayPool<byte>.Shared;

        private readonly uint version;
        private uint iv;

        private MapleCipher(uint version, uint iv) {
            this.version = version;
            this.iv = iv;
        }

        private void AdvanceIV() {
            iv = Rand32.CrtRand(iv);
        }

        public static short EncodeSeqBase(uint version, uint iv) {
            return (short)(version ^ (iv >> 16));
        }

        public static short DecodeSeqBase(short encSeq, uint iv) {
            return (short)((iv >> 16) ^ encSeq);
        }

        public static bool TryReadFrame(ref ReadOnlySequence<byte> buffer, out short encSeq, out ReadOnlySequence<byte> payload) {
            if (buffer.Length < HEADER_SIZE) {
                encSeq = 0;
                payload = default;
                return false;
            }

            SequenceReader<byte> reader = new SequenceReader<byte>(buffer);
            reader.TryReadLittleEndian(out encSeq);
            reader.TryReadLittleEndian(out int packetSize);
            if ((uint)packetSize > (uint)MaxPacketSize) {
                throw new ArgumentException($"Packet has invalid length: {packetSize}");
            }

            long rawPacketSize = (long)HEADER_SIZE + packetSize;
            if (buffer.Length < rawPacketSize) {
                encSeq = 0;
                payload = default;
                return false;
            }

            payload = buffer.Slice(HEADER_SIZE, packetSize);
            buffer = buffer.Slice(rawPacketSize);
            return true;
        }

        public static int TryReadHeader(ReadOnlySequence<byte> buffer, out PoolByteReader? packet) {
            return TryReadHeader(buffer, out packet, out _);
        }

        public static int TryReadHeader(ReadOnlySequence<byte> buffer, out PoolByteReader? packet, out short encSeq) {
            ReadOnlySequence<byte> cursor = buffer;
            if (!TryReadFrame(ref cursor, out encSeq, out ReadOnlySequence<byte> payload)) {
                packet = null;
                return 0;
            }

            int packetSize = (int)payload.Length;
            byte[] data = ArrayProvider.Rent(packetSize);
            payload.CopyTo(data);
            packet = new PoolByteReader(ArrayProvider, data, packetSize);
            return HEADER_SIZE + packetSize;
        }

        public static ByteReader ReadHeader(byte[] rawPacket, int offset = 0) {
            return ReadHeader(rawPacket, out _, offset);
        }

        public static ByteReader ReadHeader(byte[] rawPacket, out short encSeq, int offset = 0) {
            SpanByteReader payload = ReadHeaderSpan(rawPacket.AsSpan(offset), out encSeq);
            return new ByteReader(payload.Span.ToArray());
        }

        public static SpanByteReader ReadHeader(ReadOnlySpan<byte> rawPacket, out short encSeq) {
            return ReadHeaderSpan(rawPacket, out encSeq);
        }

        public static SpanByteReader ReadHeaderSpan(ReadOnlySpan<byte> rawPacket) {
            return ReadHeaderSpan(rawPacket, out _);
        }

        public static SpanByteReader ReadHeaderSpan(ReadOnlySpan<byte> rawPacket, out short encSeq) {
            if (rawPacket.Length < HEADER_SIZE) {
                throw new ArgumentException($"Packet has invalid length: {rawPacket.Length}");
            }

            encSeq = BinaryPrimitives.ReadInt16LittleEndian(rawPacket);
            int packetSize = BinaryPrimitives.ReadInt32LittleEndian(rawPacket.Slice(2));
            if ((uint)packetSize > (uint)MaxPacketSize || rawPacket.Length - HEADER_SIZE < packetSize) {
                throw new ArgumentException($"Packet has invalid length: {rawPacket.Length}");
            }

            return new SpanByteReader(rawPacket.Slice(HEADER_SIZE, packetSize));
        }

        private static List<ICrypter> InitCryptSeq(uint version, uint blockIV) {
            ICrypter?[] crypt = new ICrypter?[4];
            crypt[RearrangeCrypter.GetIndex(version)] = new RearrangeCrypter();
            crypt[XORCrypter.GetIndex(version)] = new XORCrypter(version);
            crypt[TableCrypter.GetIndex(version)] = new TableCrypter(version);

            List<ICrypter> cryptSeq = new List<ICrypter>();
            while (blockIV > 0) {
                uint index = blockIV % 10;
                if (index < crypt.Length && crypt[index] is ICrypter crypter) {
                    cryptSeq.Add(crypter);
                }
                blockIV /= 10;
            }

            return cryptSeq;
        }

        // These classes are used for encryption/decryption
        public class Encryptor {
            private readonly MapleCipher cipher;
            private ICrypter[] encryptSeq;

            public uint Version => cipher.version;
            public uint IV => cipher.iv;
            public uint BlockIV { get; private set; }

            public Encryptor(uint version, uint iv, uint blockIV) {
                this.cipher = new MapleCipher(version, iv);
                this.BlockIV = blockIV;
                this.encryptSeq = InitCryptSeq(version, blockIV).ToArray();
            }

            public void Rekey(uint iv, uint blockIV) {
                cipher.iv = iv;
                BlockIV = blockIV;
                encryptSeq = InitCryptSeq(cipher.version, blockIV).ToArray();
            }

            public short PeekSeqBase() {
                return MapleCipher.EncodeSeqBase(cipher.version, cipher.iv);
            }

            private short EncodeSeqBase() {
                short encSeq = MapleCipher.EncodeSeqBase(cipher.version, cipher.iv);
                cipher.AdvanceIV();
                return encSeq;
            }

            public void EncryptBuffer(Span<byte> payload) {
                foreach (ICrypter crypter in encryptSeq) {
                    crypter.Encrypt(payload);
                }
            }

            public short EncryptPayload(Span<byte> payload) {
                short encSeq = EncodeSeqBase();
                EncryptBuffer(payload);
                return encSeq;
            }

            public PoolByteWriter WriteHeader(byte[] packet, int offset, int length) {
                return WriteHeader(packet.AsSpan(offset, length));
            }

            public PoolByteWriter WriteHeader(ReadOnlySpan<byte> packet) {
                short encSeq = EncodeSeqBase();

                var writer = new PoolByteWriter(packet.Length + HEADER_SIZE, ArrayProvider);
                writer.WriteShort(encSeq);
                writer.WriteInt(packet.Length);
                writer.WriteBytes(packet);

                return writer;
            }

            public PoolByteWriter WriteHeader(scoped in SpanByteWriter packet) {
                return WriteHeader(packet.WrittenSpan);
            }

            public void WriteHeader(ReadOnlySpan<byte> packet, Span<byte> destination) {
                int totalSize = HEADER_SIZE + packet.Length;
                if (destination.Length < totalSize) {
                    throw new ArgumentException($"Destination span is too small: {destination.Length} < {totalSize}");
                }

                short encSeq = EncodeSeqBase();
                BinaryPrimitives.WriteInt16LittleEndian(destination, encSeq);
                BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(2), packet.Length);
                packet.CopyTo(destination.Slice(HEADER_SIZE, packet.Length));
            }

            public void WriteHeader(ReadOnlySpan<byte> packet, IBufferWriter<byte> output) {
                int totalSize = HEADER_SIZE + packet.Length;
                Span<byte> dest = output.GetSpan(totalSize);
                WriteHeader(packet, dest);
                output.Advance(totalSize);
            }

            public PoolByteWriter Encrypt(byte[] packet, int offset, int length) {
                return Encrypt(packet.AsSpan(offset, length));
            }

            public PoolByteWriter Encrypt(ReadOnlySpan<byte> packet) {
                PoolByteWriter result = WriteHeader(packet);
                EncryptBuffer(result.Buffer.AsSpan(HEADER_SIZE, packet.Length));
                return result;
            }

            public PoolByteWriter Encrypt(scoped in SpanByteWriter packet) {
                return Encrypt(packet.WrittenSpan);
            }

            public ByteWriter Encrypt(ByteWriter packet) {
                return Encrypt(packet.Buffer, 0, packet.Length).Managed();
            }

            public void Encrypt(ReadOnlySpan<byte> packet, Span<byte> destination) {
                WriteHeader(packet, destination);
                EncryptBuffer(destination.Slice(HEADER_SIZE, packet.Length));
            }

            public void Encrypt(ReadOnlySpan<byte> packet, IBufferWriter<byte> output) {
                int totalSize = HEADER_SIZE + packet.Length;
                Span<byte> dest = output.GetSpan(totalSize);
                Encrypt(packet, dest);
                output.Advance(totalSize);
            }

            public void Encrypt(scoped in SpanByteWriter packet, IBufferWriter<byte> output) {
                Encrypt(packet.WrittenSpan, output);
            }
        }

        public class Decryptor {
            private readonly MapleCipher cipher;
            private ICrypter[] decryptSeq;

            public uint Version => cipher.version;
            public uint IV => cipher.iv;
            public uint BlockIV { get; private set; }

            public Decryptor(uint version, uint iv, uint blockIV) {
                this.cipher = new MapleCipher(version, iv);
                this.BlockIV = blockIV;
                this.decryptSeq = BuildDecryptSeq(version, blockIV);
            }

            // Creates a client-side decryptor pre-stepped past the server's unencrypted handshake packet.
            public static Decryptor CreateClient(uint version, uint recvIv, uint blockIV) {
                return new Decryptor(version, Rand32.CrtRand(recvIv), blockIV);
            }

            public void Rekey(uint iv, uint blockIV, bool advanceIV = false) {
                cipher.iv = advanceIV ? Rand32.CrtRand(iv) : iv;
                BlockIV = blockIV;
                decryptSeq = BuildDecryptSeq(cipher.version, blockIV);
            }

            private static ICrypter[] BuildDecryptSeq(uint version, uint blockIV) {
                List<ICrypter> cryptSeq = InitCryptSeq(version, blockIV);
                cryptSeq.Reverse();
                return cryptSeq.ToArray();
            }

            public short PeekSeqBase(short encSeq) {
                return MapleCipher.DecodeSeqBase(encSeq, cipher.iv);
            }

            private short DecodeSeqBase(short encSeq) {
                short decSeq = MapleCipher.DecodeSeqBase(encSeq, cipher.iv);
                cipher.AdvanceIV();
                return decSeq;
            }

            public void DecryptBuffer(Span<byte> payload) {
                foreach (ICrypter crypter in decryptSeq) {
                    crypter.Decrypt(payload);
                }
            }

            public void DecryptPayload(Span<byte> payload, short encSeq) {
                short decSeq = DecodeSeqBase(encSeq);
                if (decSeq != (short)cipher.version) {
                    throw new ArgumentException($"Packet has invalid sequence header: {decSeq}");
                }

                DecryptBuffer(payload);
            }

            // For use with System.IO.Pipelines
            // Decrypt packets directly from underlying data stream without needing to buffer
            public int TryDecrypt(ReadOnlySequence<byte> buffer, out PoolByteReader? packet) {
                ReadOnlySequence<byte> cursor = buffer;
                if (!TryReadFrame(ref cursor, out short encSeq, out ReadOnlySequence<byte> payload)) {
                    packet = null;
                    return 0;
                }

                // Only decode sequence once we know there is sufficient data because it mutates IV
                short decSeq = DecodeSeqBase(encSeq);
                if (decSeq != (short)cipher.version) {
                    throw new ArgumentException($"Packet has invalid sequence header: {decSeq}");
                }

                int packetSize = (int)payload.Length;
                byte[] data = ArrayProvider.Rent(packetSize);
                payload.CopyTo(data);
                DecryptBuffer(data.AsSpan(0, packetSize));

                packet = new PoolByteReader(ArrayProvider, data, packetSize);
                return HEADER_SIZE + packetSize;
            }

            public SpanByteReader DecryptInPlace(Span<byte> rawPacket) {
                if (rawPacket.Length < HEADER_SIZE) {
                    throw new ArgumentException($"Packet has invalid length: {rawPacket.Length}");
                }

                short encSeq = BinaryPrimitives.ReadInt16LittleEndian(rawPacket);
                int packetSize = BinaryPrimitives.ReadInt32LittleEndian(rawPacket.Slice(2));
                if ((uint)packetSize > (uint)MaxPacketSize || rawPacket.Length - HEADER_SIZE < packetSize) {
                    throw new ArgumentException($"Packet has invalid length: {rawPacket.Length}");
                }

                Span<byte> payload = rawPacket.Slice(HEADER_SIZE, packetSize);
                DecryptPayload(payload, encSeq);
                return new SpanByteReader(payload);
            }

            public ByteReader Decrypt(byte[] rawPacket, int offset = 0) {
                if ((uint)offset > (uint)rawPacket.Length || rawPacket.Length - offset < HEADER_SIZE) {
                    throw new ArgumentException($"Packet has invalid length: {rawPacket.Length}");
                }

                var reader = new ByteReader(rawPacket, offset);
                short encSeq = reader.ReadShort();
                int packetSize = reader.ReadInt();
                if ((uint)packetSize > (uint)MaxPacketSize || reader.Available < packetSize) {
                    throw new ArgumentException($"Packet has invalid length: {rawPacket.Length}");
                }

                short decSeq = DecodeSeqBase(encSeq);
                if (decSeq != (short)cipher.version) {
                    throw new ArgumentException($"Packet has invalid sequence header: {decSeq}");
                }

                byte[] packet = reader.ReadBytes(packetSize);
                DecryptBuffer(packet);

                return new ByteReader(packet);
            }
        }
    }
}
