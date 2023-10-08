using ExoticServer.App;
using ExoticServer.Classes.Server.Authentication;
using Newtonsoft.Json;
using System.Text;

namespace ExoticServer.Classes.Server.PacketSystem.Packets
{
    public class UserDetailsRequestPacket : IPacketHandler
    {
        private ExoticTcpServer _tcpServer;
        private PacketHandler _serverPacketHandler;

        public async void Handle(Packet packet, ClientHandler clientHandler)
        {
            _tcpServer = clientHandler.GetTcpServer();
            _serverPacketHandler = _tcpServer.ServerPacketHandler;

            string data = Encoding.UTF8.GetString(packet.Data);

            string[] spitData = data.Split(':');

            string username = spitData[0].Trim();

            UserDetails requestedUserDetails = await ChronicApplication.Instance.Database.GetUserDetailsByUsername(username);

            string userDetailsJsonString = JsonConvert.SerializeObject(requestedUserDetails);
            byte[] userDetailsData = Encoding.UTF8.GetBytes(userDetailsJsonString);

            Packet requestedUserDetailsPacket = _serverPacketHandler.CreateNewPacket(userDetailsData, "Requested UserDetails Response Packet");
            await _serverPacketHandler.SendPacketAsync(requestedUserDetailsPacket, clientHandler.GetNetworkStream());
        }
    }
}
