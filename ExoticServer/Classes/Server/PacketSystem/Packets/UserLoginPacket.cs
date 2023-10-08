using ExoticServer.Classes.Server.Authentication;
using Newtonsoft.Json;
using System.Text;  

namespace ExoticServer.Classes.Server.PacketSystem.Packets
{
    public class UserLoginPacket : IPacketHandler
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

            Packet loginResponsePacket = null;

            if (await AuthenticationService.AuthenticateUser(userAuthDetails.Username, userAuthDetails.PasswordHash))
            {
                // Send a successful login packet as a response
                byte[] dataBytes = Encoding.UTF8.GetBytes("Login Successful");
                loginResponsePacket = _serverPacketHandler.CreateNewPacket(dataBytes, "Login Response Packet", true);
                await _serverPacketHandler.SendPacketAsync(loginResponsePacket, clientHandler.GetNetworkStream());
            }
            else
            {
                // Send a failed login packet as a response
                byte[] dataBytes = Encoding.UTF8.GetBytes("Login Failed");
                loginResponsePacket = _serverPacketHandler.CreateNewPacket(dataBytes, "Login Response Packet", true);
                await _serverPacketHandler.SendPacketAsync(loginResponsePacket, clientHandler.GetNetworkStream());
            }
        }
    }
}
