using ExoticServer.Classes.Server.Authentication;
using Newtonsoft.Json;
using System.Text;
using System.Windows.Forms;

namespace ExoticServer.Classes.Server.PacketSystem.Packets
{
    public class UserLoginPacket : IPacketHandler
    {
        public void Handle(Packet packet, ClientHandler clientHandler)
        {
            // Convert bytes to string
            string jsonString = Encoding.UTF8.GetString(packet.Data);

            // Deserialize the JSON string to UserAuthDetails object
            UserAuthDetails userAuthDetails = JsonConvert.DeserializeObject<UserAuthDetails>(jsonString);

            if(AuthenticationService.AuthenticateUser(userAuthDetails.Username, userAuthDetails.PasswordHash))
            {
                // Send a successful login packet as a response
                MessageBox.Show("Found User! Logging in...");
            }
            else
            {
                // Send a failed login packet as a response
                MessageBox.Show("Fuck off RAT.");
            }
        }
    }
}
