using ExoticServer.App;
using ExoticServer.Classes.Utils;
using Serilog;
using System;
using System.Threading.Tasks;

namespace ExoticServer.Classes.Server.Authentication
{
    public static class AuthenticationService
    {
        public static async Task<bool> RegisterUser(UserDetails userDetails, string clientHashedPassword)
        {
            try
            {
                UserDetails existingUser = await ChronicApplication.Instance.Database.GetUserDetailsByEmail(userDetails.Email);

                if (existingUser != null)
                {
                    // User with the same email already exists.
                    // Send a user already exist packet
                    Log.Logger.Warning("(ClientHandler) AuthenticateUser - Email already exists.");
                    return false;
                }

                string salt = PasswordHelper.CreateSalt();
                string serverHashedPassword = PasswordHelper.HashPassword(clientHashedPassword, salt); // makes it a double hashed password that is salted

                UserAuthDetails userAuth = new UserAuthDetails
                {
                    UserID = userDetails.UserID,
                    Username = userDetails.Username,
                    PasswordHash = serverHashedPassword,
                    PasswordSalt = salt
                };

                await ChronicApplication.Instance.Database.AddUserDetails(userDetails);
                await ChronicApplication.Instance.Database.AddUserAuthentication(userAuth);

                return true;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"(ClientHandler) RegisterUser - Error registering user: {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> AuthenticateUser(string username, string clientHashedPassword)
        {
            try
            {
                UserDetails userDetails = await ChronicApplication.Instance.Database.GetUserDetailsByUsername(username);

                if (userDetails == null)
                {
                    // No such user exists.
                    //Send a Wrong user/password response packet
                    ChronicApplication.Instance.Logger.Error("(ClientHandler) AuthenticateUser - No Such User.");
                    return false;
                }

                UserAuthDetails userAuth = await ChronicApplication.Instance.Database.GetUserAuthenticationByUserID(userDetails.UserID);

                string serverHashedPassword = PasswordHelper.HashPassword(clientHashedPassword, userAuth.PasswordSalt);

                if (serverHashedPassword == userAuth.PasswordHash)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.Logger.Error($"(ClientHandler) AuthenticateUser - Error authenticating user: {ex.Message}");
                return false;
            }
        }
    }
}
