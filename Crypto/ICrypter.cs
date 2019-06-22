namespace MaplePacketLib2.Crypto {
    public interface ICrypter {
        void Init(uint version);
        void Encrypt(byte[] src, uint seqKey);
        void Decrypt(byte[] src, uint seqKey);
    }
}
