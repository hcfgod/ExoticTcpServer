using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace ExoticServer.Classes.Server.PacketSystem.Packets
{
    public class ClientPublicKeyPacket : IPacketHandler
    {
        public async void Handle(Packet packet, ClientHandler clientHandler)
        {
            string clientPublicKeyJson = Encoding.UTF8.GetString(packet.Data);
            RSAParameters clientPublicKey = JsonConvert.DeserializeObject<RSAParameters>(clientPublicKeyJson);

            clientHandler.GetTcpServer().ServerKeyManager.SetClientPublicKey(clientPublicKey);

            byte[] aesKey = CryptoUtility.AesKey;
            byte[] aesIV = CryptoUtility.AesIV;

            byte[] encryptedKeyBytes = CryptoUtility.RsaEncrypt(aesKey, clientPublicKey);

            Packet encryptedKeyPacket = clientHandler.GetTcpServer().ServerPacketHandler.CreateNewPacket(encryptedKeyBytes, "Aes Key Packet");
            await clientHandler.GetTcpServer().ServerPacketHandler.SendPacketAsync(encryptedKeyPacket, clientHandler.GetNetworkStream());

            byte[] encryptedIVBytes = CryptoUtility.RsaEncrypt(aesIV, clientPublicKey);

            Packet encryptedIVPacket = clientHandler.GetTcpServer().ServerPacketHandler.CreateNewPacket(encryptedIVBytes, "Aes IV Packet");
            await clientHandler.GetTcpServer().ServerPacketHandler.SendPacketAsync(encryptedIVPacket, clientHandler.GetNetworkStream());
        }
    }
}
