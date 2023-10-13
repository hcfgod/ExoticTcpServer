using ExoticServer.Classes.Server.Authentication;
using Newtonsoft.Json;
using System.Text;  

namespace ExoticServer.Classes.Server.PacketSystem.PacketHandlers
{
    public class UserLoginPacketHandler : IPacketHandler
    {
        private ExoticTcpServer _tcpServer;
        private PacketHandler _serverPacketHandler;

        public async void Handle(Packet packet, ClientHandler clientHandler)
        {
            _tcpServer = clientHandler.GetTcpServer();
            _serverPacketHandler = _tcpServer.ServerPacketHandler;

            // Convert bytes to string
            string jsonString = Encoding.UTF8.GetString(packet.Data);

            // Deserialize the JSON string to UserAuthDetails object
            UserAuthDetails userAuthDetails = JsonConvert.DeserializeObject<UserAuthDetails>(jsonString);

            if (await AuthenticationService.AuthenticateUser(userAuthDetails.Username, userAuthDetails.PasswordHash))
            {
                // Send a successful login packet as a response
                byte[] dataBytes = Encoding.UTF8.GetBytes("Login Successful");
                await _serverPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), dataBytes, "Login Response", true);
            }
            else
            {
                // Send a failed login packet as a response
                byte[] dataBytes = Encoding.UTF8.GetBytes("Login Failed");
                await _serverPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), dataBytes, "Login Response", true);
            }
        }
    }
}
