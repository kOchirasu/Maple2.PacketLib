Maple2.PacketLib
===============
Packet Library for MapleStory2

Credits to [@EricSoftTM](https://github.com/EricSoftTM) for packet encryption algos

### General Usage:
- SIV: Send IV
- RIV: Recv IV
- BLOCK_IV: Determines cipher ordering

Sending Packets
```C#
var sendCipher = new MapleCipher.Encryptor(VERSION, SIV, BLOCK_IV);
...
// Handshake is NOT encrypted
PacketWriter writer = PacketWriter.Of(HANDSHAKE_OPCODE);
Packet handshake = sendCipher.WriteHeader(handshake);
SendPacket(handshake);
...
PacketWriter writer = PacketWriter.Of(OPCODE);
writer.Write(DATA);

Packet encryptedPacket = sendCipher.Encrypt(writer);
SendPacket(encryptedPacket);
```

Receiving Packets
```C#
var recvCipher = new MapleCipher.Decryptor(VERSION, RIV, BLOCK_IV);
MapleStream stream = new MapleStream();
...
while (IS_READING) {
    var data = bytes from network;
    stream.Write(data);
    
    while (stream.TryRead(out byte[]? packetBuffer)) {
        ByteReader decryptedPacket = recvCipher.Decrypt(packetBuffer);
        OnPacket(decryptedPacket);
    }
}
```

Client Handshake & Zero-Allocation `ref struct` Usage (`SpanByteReader` / `SpanByteWriter`)
```C#
// 1. Read unencrypted server handshake frame
SpanByteReader handshake = MapleCipher.ReadHeaderSpan(rawHandshakeFrame, out short encSeq);
ushort opcode = handshake.Read<ushort>();
uint version = handshake.Read<uint>();
uint sendIv = handshake.Read<uint>();
uint recvIv = handshake.Read<uint>();
uint blockIv = handshake.Read<uint>();

var sendCipher = new MapleCipher.Encryptor(version, sendIv, blockIv);
var recvCipher = MapleCipher.Decryptor.CreateClient(version, recvIv, blockIv);

// 2. Write & encrypt without heap allocations
using var writer = new SpanByteWriter(stackalloc byte[128], ArrayPool<byte>.Shared);
writer.Write<ushort>(OPCODE);
writer.Write(DATA);
sendCipher.Encrypt(writer, pipeWriter);

// 3. Decrypt in-place into a stack-only SpanByteReader
SpanByteReader packet = recvCipher.DecryptInPlace(rawPacketSpan);
```
