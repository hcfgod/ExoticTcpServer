using ExoticServer.App;
using ExoticServer.Classes.Server.Authentication;
using ExoticServer.Classes.Utils;
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

            if(!EmailValidator.IsValidEmail(userDetails.Email))
            {
                // Send a response packet saying must enter a valid email
                return;
            }

            if (await ChronicApplication.Instance.Database.DoesUsernameExist(userDetails.Username))
            {
                // Send a response packet saying username already exist
                return;
            }

            if (await ChronicApplication.Instance.Database.DoesEmailExist(userDetails.Email))
            {
                // Send a response packet saying email already exist
                return;
            }

            if (await AuthenticationService.RegisterUser(userDetails, userAuthDetails.PasswordHash))
            {
                // Send a response packet letting the user know they successfully registered
            }
            else
            {
                // Send a response packet letting the user know the registration failed
            }
        }
    }
}
