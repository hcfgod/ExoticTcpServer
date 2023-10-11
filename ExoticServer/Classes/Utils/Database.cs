using ExoticServer.Classes.Server;
using ExoticServer.Classes.Server.Authentication;
using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Threading.Tasks;

namespace ExoticServer.Classes.Utils
{
    public class Database
    {
        private readonly string _connectionString;
        private MySqlConnection _connection;

        public Database(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task AddUserDetails(UserDetails user)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);

                await _connection.OpenAsync();

                string query = "INSERT INTO usersdetails (UserID, Username, Email) VALUES (@UserID, @Username, @Email)";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserID", user.UserID);
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Email", user.Email);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) AddUserDetails: Exception Caught - " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
        public async Task AddUserAuthentication(UserAuthDetails userAuthentication)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);

                await _connection.OpenAsync();

                string query = "INSERT INTO usersauth (UserID, Username, PasswordHash, PasswordSalt) VALUES (@UserID, @Username, @PasswordHash, @PasswordSalt)";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserID", userAuthentication.UserID);
                cmd.Parameters.AddWithValue("@Username", userAuthentication.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", userAuthentication.PasswordHash);
                cmd.Parameters.AddWithValue("@PasswordSalt", userAuthentication.PasswordSalt);

                await cmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) AddUserAuthentication: Exception Caught - " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<UserDetails> GetUserDetailsByUsername(string username)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                await _connection.OpenAsync();

                string query = "SELECT * FROM usersdetails WHERE Username = @Username";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@Username", username);

                MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new UserDetails
                    {
                        UserID = reader.GetString("UserID"),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) GetUserDetailsByUsername: Exception Caught - " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }
            return null;
        }

        public async Task<UserDetails> GetUserDetailsByEmail(string email)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                await _connection.OpenAsync();

                string query = "SELECT * FROM userdetails WHERE Email = @Email";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@Email", email);

                MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new UserDetails
                    {
                        UserID = reader.GetString("UserID"),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                    };
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) GetUserDetailsByEmail: Exception Caught - " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }
            return null;
        }

        public async Task<UserDetails> GetUserDetailsByUserID(string userID)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                await _connection.OpenAsync();

                string query = "SELECT * FROM usersdetails WHERE UserID = @UserID";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserID", userID);

                MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                UserDetails userDetails = null;

                if (await reader.ReadAsync())
                {
                    userDetails = new UserDetails
                    {
                        UserID = reader.GetString("UserID"),
                        Username = reader.GetString("Username"),
                        Email = reader.GetString("Email"),
                    };
                }

                return userDetails;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) GetUserDetailsByUserID: Exception Caught - " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }
            return null;
        }

        public async Task<UserAuthDetails> GetUserAuthenticationByUserID(string userID)
        {
            try
            {
                // Opening a connection
                _connection = new MySqlConnection(_connectionString);
                await _connection.OpenAsync();

                // Getting User auth details
                string userAuthQuery = "SELECT * FROM usersauth WHERE UserID = @UserID";
                MySqlCommand userAuthCmd = new MySqlCommand(userAuthQuery, _connection);

                userAuthCmd.Parameters.AddWithValue("@UserID", userID);

                MySqlDataReader userAuthReader = (MySqlDataReader)await userAuthCmd.ExecuteReaderAsync();

                UserAuthDetails userAuth = null;

                if (await userAuthReader.ReadAsync())
                {
                    userAuth = new UserAuthDetails
                    {
                        UserID = userAuthReader.GetString("UserID"),
                        Username = userAuthReader.GetString("Username"),
                        PasswordHash = userAuthReader.GetString("PasswordHash"),
                        PasswordSalt = userAuthReader.GetString("PasswordSalt"),
                    };
                }

                return userAuth;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) GetUserAuthenticationByUserID: Exception Caught - " + ex.Message);
            }
            finally
            {
                await _connection.CloseAsync();
            }
            return null;
        }

        public async Task<bool> DoesUsernameExist(string username)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                await _connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM usersdetails WHERE Username = @Username";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@Username", username);

                object result = await cmd.ExecuteScalarAsync();
                int count = Convert.ToInt32(result);

                return count > 0;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) DoesUsernameExist: Exception Caught - " + ex.Message);
                return false;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }

        public async Task<bool> DoesEmailExist(string email)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                await _connection.OpenAsync();

                string query = "SELECT COUNT(*) FROM usersdetails WHERE Email = @Email";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@Email", email);

                object result = await cmd.ExecuteScalarAsync();
                int count = Convert.ToInt32(result);

                return count > 0;
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) DoesEmailExist: Exception Caught - " + ex.Message);
                return false;
            }
            finally
            {
                await _connection.CloseAsync();
            }
        }
    }
}
