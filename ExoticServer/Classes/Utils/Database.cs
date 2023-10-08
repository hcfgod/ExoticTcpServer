using ExoticServer.Classes.Server;
using ExoticServer.Classes.Server.Authentication;
using MySql.Data.MySqlClient;
using Serilog;
using System;

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

        public void AddUserDetails(UserDetails user)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);

                _connection.Open();

                string query = "INSERT INTO usersdetails (UserID, Username, Email) VALUES (@UserID, @Username, @Email)";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserID", user.UserID);
                cmd.Parameters.AddWithValue("@Username", user.Username);
                cmd.Parameters.AddWithValue("@Email", user.Email);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) AddUserDetails: Exception Caught - " + ex.Message);
            }
            finally
            {
                _connection.Close();
                _connection.Dispose();
            }
        }
        public void AddUserAuthentication(UserAuthDetails userAuthentication)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);

                _connection.Open();

                string query = "INSERT INTO usersauth (UserID, Username, PasswordHash, PasswordSalt) VALUES (@UserID, @Username, @PasswordHash, @PasswordSalt)";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserID", userAuthentication.UserID);
                cmd.Parameters.AddWithValue("@Username", userAuthentication.Username);
                cmd.Parameters.AddWithValue("@PasswordHash", userAuthentication.PasswordHash);
                cmd.Parameters.AddWithValue("@PasswordSalt", userAuthentication.PasswordSalt);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("(Database.cs) AddUserAuthentication: Exception Caught - " + ex.Message);
            }
            finally
            {
                _connection.Close();
            }
        }

        public UserDetails GetUserDetailsByUsername(string username)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                _connection.Open();

                string query = "SELECT * FROM usersdetails WHERE Username = @Username";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@Username", username);

                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
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
                _connection.Close();
                _connection.Dispose();
            }
            return null;
        }

        public UserDetails GetUserDetailsByEmail(string email)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                _connection.Open();

                string query = "SELECT * FROM userdetails WHERE Email = @Email";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@Email", email);

                MySqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
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
                _connection.Close();
                _connection.Dispose();
            }
            return null;
        }

        public UserDetails GetUserDetailsByUserID(string userID)
        {
            try
            {
                _connection = new MySqlConnection(_connectionString);
                _connection.Open();

                string query = "SELECT * FROM usersdetails WHERE UserID = @UserID";

                MySqlCommand cmd = new MySqlCommand(query, _connection);
                cmd.Parameters.AddWithValue("@UserID", userID);

                MySqlDataReader reader = cmd.ExecuteReader();

                UserDetails userDetails = null;

                if (reader.Read())
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
                _connection.Close();
                _connection.Dispose();
            }
            return null;
        }

        public UserAuthDetails GetUserAuthenticationByUserID(string userID)
        {
            try
            {

                UserDetails userDetails = GetUserDetailsByUserID(userID);

                _connection = new MySqlConnection(_connectionString);
                _connection.Open();

                string query = "SELECT * FROM usersauth WHERE UserID = @UserID";
                MySqlCommand cmd = new MySqlCommand(query, _connection);

                cmd.Parameters.AddWithValue("@UserID", userID);

                MySqlDataReader reader = cmd.ExecuteReader();

                UserAuthDetails userAuth = null;

                if (reader.Read())
                {
                    userAuth = new UserAuthDetails
                    {
                        UserID = reader.GetString("UserID"),
                        Username = reader.GetString("Username"),
                        PasswordHash = reader.GetString("PasswordHash"),
                        PasswordSalt = reader.GetString("PasswordSalt"),
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
                _connection.Close();
                _connection.Dispose();
            }
            return null;
        }
    }
}
