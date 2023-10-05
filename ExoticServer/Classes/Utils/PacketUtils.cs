using ExoticServer.Classes.Server.PacketSystem;
using System.Net.Sockets;
using System.Text;

namespace ExoticServer.Classes.Utils
{
    public class PacketUtils
    {
        public static async void SendSecurityDisconnectionPacket(PacketHandler packetHandler, NetworkStream networkStream)
        {
            string data = "Disconnected For Security Purposes";
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            Packet securityDisconnectionPacket = packetHandler.CreateNewPacket(dataBytes, "Disconnected For Security Reasons Packet");
            await packetHandler.SendPacketAsync(securityDisconnectionPacket, networkStream);
        }

        public static async void SendTooManyRequestPacket(PacketHandler packetHandler, NetworkStream networkStream)
        {
            string data = "Please wait 1 minute before sending another request.";
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            Packet tooManyRequestPacket = packetHandler.CreateNewPacket(dataBytes, "Too Many Request Packet");
            await packetHandler.SendPacketAsync(tooManyRequestPacket, networkStream);
        }
    }
}
