namespace MaplePacketLib2.Crypto {
    public interface ICrypter {
        void Encrypt(byte[] src);
        void Encrypt(byte[] src, int offset, int count);
        void Decrypt(byte[] src);
        void Decrypt(byte[] src, int offset, int count);
    }
}
