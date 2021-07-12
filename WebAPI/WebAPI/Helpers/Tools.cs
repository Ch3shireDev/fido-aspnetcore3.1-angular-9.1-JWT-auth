using System;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using WebAPI.Services;

namespace WebAPI
{
    public static class Tools
    {
        public static string GetHash(string password, string salt)
        {
            string hash;

            using (var mysha256 = SHA256.Create())
            {
                var chars = Encoding.UTF8.GetBytes((salt + password).ToCharArray());
                var hashBytes = mysha256.ComputeHash(chars);
                hash = Encoding.UTF8.GetString(hashBytes);
            }

            return hash;
        }

        public static string GetSalt()
        {
            var rngCsp = new RNGCryptoServiceProvider();
            var byteArray = new byte[8];
            rngCsp.GetBytes(byteArray);
            return Encoding.UTF8.GetString(byteArray);
        }

        public static User GetUser(string username, string ConnectionString)
        {
            User user = null;
            using (var connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "select id, username, USER_ID, password_hash, password_salt, display_name from USERS where username = @username";
                    cmd.Parameters.AddWithValue("@username", username);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        user = new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            UserId = reader.GetString(2),
                            PasswordHash = reader.GetString(3),
                            PasswordSalt = reader.GetString(4),
                            DisplayName = reader.GetString(5)
                        };
                    reader.Close();
                }

                connection.Close();
            }

            return user;
        }

        public static User CreateUser(string username, string password, string displayName, string connectionString)
        {
            var userId = Convert.ToBase64String(Encoding.UTF8.GetBytes(username));
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                var salt = GetSalt();
                var hash = GetHash(password, salt);

                var cmd2 = connection.CreateCommand();
                cmd2.CommandText =
                    "insert into USERS " +
                    "(username, user_id, password_hash, password_salt, display_name) " +
                    "values " +
                    "(@username, @user_id, @password_hash, @password_salt, @display_name)";

                cmd2.Parameters.AddWithValue("@username", username);
                cmd2.Parameters.AddWithValue("@user_id", userId);
                cmd2.Parameters.AddWithValue("@password_hash", hash);
                cmd2.Parameters.AddWithValue("@password_salt", salt);
                cmd2.Parameters.AddWithValue("@display_name", displayName);

                cmd2.ExecuteNonQuery();
                connection.Close();
            }

            return GetUser(username, connectionString);
        }

        public static User GetUserById(int id, string connectionString)
        {
            User user = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "select id, username, USER_ID, password_hash, password_salt, display_name from USERS where id=@id";
                    cmd.Parameters.AddWithValue("@id", id);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        user = new User
                        {
                            Id = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            UserId = reader.GetString(2),
                            PasswordHash = reader.GetString(3),
                            PasswordSalt = reader.GetString(4),
                            DisplayName = reader.GetString(5)
                        };
                    reader.Close();
                }

                connection.Close();
            }

            return user;
        }
    }
}

