using System.Text;

namespace ExoticServer.Classes.Server.PacketSystem.Packets
{
    public class ClientPublicKeyPacket : IPacketHandler
    {
        public async void Handle(Packet packet, ClientHandler clientHandler)
        {
            string clientPublicKey = Encoding.UTF8.GetString(packet.Data);
            clientHandler.GetTcpServer().ServerKeyManager.SetClientPublicKey(clientPublicKey);

            string aesKey = CryptoUtility.AesKey;
            string aesIV = CryptoUtility.AesIV;

            byte[] keyAndIVBytes = Encoding.UTF8.GetBytes($"{aesKey} : {aesIV}");
            byte[] encryptedKeyAndIVBytes = CryptoUtility.EncryptWithPublicKey(keyAndIVBytes, clientPublicKey);

            Packet encryptedKeyAndIVPacket = clientHandler.GetTcpServer().ServerPacketHandler.CreateNewPacket(encryptedKeyAndIVBytes, "Aes Key And IV Packet");
            await clientHandler.GetTcpServer().ServerPacketHandler.SendPacketAsync(encryptedKeyAndIVPacket, clientHandler.GetNetworkStream());
        }
    }
}
