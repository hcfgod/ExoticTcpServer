using ExoticServer.Classes.Server.Authentication;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Windows.Forms;

namespace ExoticServer.Classes.Server.PacketSystem.Packets
{
    public class UserRegistrationPacket : IPacketHandler
    {
        public async void Handle(Packet packet, ClientHandler clientHandler)
        {
            // Convert bytes to string
            string jsonString = Encoding.UTF8.GetString(packet.Data);

            string[] splitJsonString = jsonString.Split(new[] { "-newpacket-" }, StringSplitOptions.None);

            string userDetailsJsonString = splitJsonString[0].Trim();
            string userAuthDeatilsJsonString = splitJsonString[1].Trim();

            // Deserialize the JSON string to UserAuthDetails object
            UserDetails userDetails = JsonConvert.DeserializeObject<UserDetails>(userDetailsJsonString);
            UserAuthDetails userAuthDetails = JsonConvert.DeserializeObject<UserAuthDetails>(userAuthDeatilsJsonString);

            if (await AuthenticationService.RegisterUser(userDetails, userAuthDetails.PasswordHash))
            {
                // Send a successful register packet as a response
                MessageBox.Show("Registering User!");
            }
            else
            {
                // Send a failed registration packet as a response
                MessageBox.Show("Fuck off RAT.");
            }
        }
    }
}
