using ExoticServer.App;
using ExoticServer.Classes.Server.Authentication;
using ExoticServer.Classes.Utils;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Windows.Forms;

namespace ExoticServer.Classes.Server.PacketSystem.PacketHandlers
{
    public class UserRegistrationPacketHandler : IPacketHandler
    {
        private PacketHandler _serverPacketHandler;

        public async void Handle(Packet packet, ClientHandler clientHandler)
        {
            _serverPacketHandler = clientHandler.GetTcpServer().ServerPacketHandler;

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
                byte[] mustEnterValidEmailBytes = Encoding.UTF8.GetBytes($"You must enter a valid email.");
                await _serverPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), mustEnterValidEmailBytes, "Must Enter Valid Email", true);

                return;
            }

            if (await ChronicApplication.Instance.Database.DoesUsernameExist(userDetails.Username))
            {
                // Send a response packet saying username already exist
                byte[] usernameAlreadyExistBytes = Encoding.UTF8.GetBytes($"{userDetails.Username} is already in use, Please try a different username.");
                await _serverPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), usernameAlreadyExistBytes, "Username Already Exist", true);
                return;
            }

            if (await ChronicApplication.Instance.Database.DoesEmailExist(userDetails.Email))
            {
                // Send a response packet saying email already exist
                byte[] emailAlreadyExistBytes = Encoding.UTF8.GetBytes($"{userDetails.Email} is already in use, Please try a different email.");
                await _serverPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), emailAlreadyExistBytes, "Email Already Exist", true);
                return;
            }

            if (await AuthenticationService.RegisterUser(userDetails, userAuthDetails.PasswordHash))
            {
                // Send a response packet letting the user know they successfully registered
                byte[] registrationSuccessfulBytes = Encoding.UTF8.GetBytes("Registration Successful");
                await _serverPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), registrationSuccessfulBytes, "Registration Response", true);
            }
            else
            {
                // Send a response packet letting the user know the registration failed
                byte[] registrationFailedBytes = Encoding.UTF8.GetBytes("Registration failed");
                await _serverPacketHandler.CreateAndSendPacket(clientHandler.GetNetworkStream(), registrationFailedBytes, "Registration Response", true);
            }
        }
    }
}
