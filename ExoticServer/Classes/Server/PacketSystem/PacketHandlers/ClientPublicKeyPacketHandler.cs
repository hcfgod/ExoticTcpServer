using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace ExoticServer.Classes.Server.PacketSystem.PacketHandlers
{
    public class ClientPublicKeyPacketHandler : IPacketHandler
    {
        public async void Handle(Packet packet, ClientHandler clientHandler)
        {
            string clientPublicKeyJson = Encoding.UTF8.GetString(packet.Data);
            RSAParameters clientPublicKey = JsonConvert.DeserializeObject<RSAParameters>(clientPublicKeyJson);

            clientHandler.GetTcpServer().ServerKeyManager.SetClientPublicKey(clientPublicKey);

            byte[] aesKey = CryptoUtility.AesKey;
            byte[] aesIV = CryptoUtility.AesIV;

            byte[] encryptedKeyBytes = CryptoUtility.RsaEncrypt(aesKey, clientPublicKey);
            await clientHandler.GetTcpServer().ServerPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), encryptedKeyBytes, "Aes Key");

            byte[] encryptedIVBytes = CryptoUtility.RsaEncrypt(aesIV, clientPublicKey);
            await clientHandler.GetTcpServer().ServerPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), encryptedIVBytes, "Aes IV");
        }
    }
}
