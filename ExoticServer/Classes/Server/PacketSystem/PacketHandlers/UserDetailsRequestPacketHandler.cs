using ExoticServer.App;
using Newtonsoft.Json;
using System.Text;

namespace ExoticServer.Classes.Server.PacketSystem.PacketHandlers
{
    public class UserDetailsRequestPacketHandler : IPacketHandler
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

            await _serverPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), userDetailsData, "Requested UserDetails Response", true);
        }
    }
}
