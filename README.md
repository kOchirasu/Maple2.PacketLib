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
    
    while (mapleStream.TryRead(out byte[] packetBuffer)) {
        Packet decryptedPacket = recvCipher.Decrypt(packetBuffer);
        OnPacket(decryptedPacket);
    }
}
```
