using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using Fido2NetLib;
using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using WebAPI.Services;

namespace WebAPI.Helpers
{
    public static class Tools
    {
        public static byte[] GetHash(string password, byte[] salt)
        {
            using var mysha256 = SHA256.Create();
            var chars = Encoding.UTF8.GetBytes((salt + password).ToCharArray());
            var hashBytes = mysha256.ComputeHash(chars);
            return hashBytes;
        }

        public static byte[] GetSalt()
        {
            var rngCsp = new RNGCryptoServiceProvider();
            var byteArray = new byte[8];
            rngCsp.GetBytes(byteArray);
            return byteArray;
        }


        //public static User CreateUser(string username, string password, string displayName, string connectionString)
        //{
        //    var userId = Convert.ToBase64String(Encoding.UTF8.GetBytes(username));
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        var salt = GetSalt();
        //        var hash = GetHash(password, salt);

        //        var cmd2 = connection.CreateCommand();
        //        cmd2.CommandText =
        //            "insert into USERS " +
        //            "(username, user_id, password_hash, password_salt, display_name) " +
        //            "values " +
        //            "(@username, @user_id, @password_hash, @password_salt, @display_name)";

        //        cmd2.Parameters.AddWithValue("@username", username);
        //        cmd2.Parameters.AddWithValue("@user_id", userId);
        //        cmd2.Parameters.AddWithValue("@password_hash", hash);
        //        cmd2.Parameters.AddWithValue("@password_salt", salt);
        //        cmd2.Parameters.AddWithValue("@display_name", displayName);

        //        cmd2.ExecuteNonQuery();
        //        connection.Close();
        //    }

        //    return GetUser(username, connectionString);
        //}

        //public static User GetUserById(int id, string connectionString)
        //{
        //    User user = null;
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        using (var cmd = connection.CreateCommand())
        //        {
        //            cmd.CommandText =
        //                "select * from USERS where id=@id";
        //            cmd.Parameters.AddWithValue("@id", id);
        //            var reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //                user = new User
        //                {
        //                    Id = reader["USER_ID"] as bytyes
        //                    Username = reader.GetString(1),
        //                    UserId = reader.GetString(2),
        //                    PasswordHash = reader.GetString(3),
        //                    PasswordSalt = reader.GetString(4),
        //                    DisplayName = reader.GetString(5)
        //                };
        //            reader.Close();
        //        }

        //        connection.Close();
        //    }

        //    return user;
        //}

        public static List<Fido2User> GetUsersByCredentialId(byte[] credentialId, string connectionString)
        {
            var users = new List<Fido2User>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select * from USERS where CREDENTIAL_ID = @CREDENTIAL_ID";
                    command.Parameters.AddWithValue("@CREDENTIAL_ID", credentialId);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                        users.Add(new Fido2User
                        {
                            DisplayName = reader["DISPLAY_NAME"] as string,
                            Id = reader["USER_ID"] as byte[],
                            Name = reader["NAME"] as string
                        });
                }

                connection.Close();
            }

            return users;
        }

        public static List<StoredCredential> GetCredentialsByUser(string username, string connectionString)
        {
            // TODO: Zaktualizować strukturę bazy danych tak, by obsługiwała relację one-to-many dla users-credetials.
            var credentials = new List<StoredCredential>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select * from USERS where USERNAME = @USERNAME";
                    command.Parameters.AddWithValue("@USERNAME", username);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var counter = (uint) (reader["SIGNATURE_COUNTER"] as int? ?? 0);

                        var credential = new StoredCredential
                        {
                            AaGuid = reader["CREDENTIAL_GUID"] as Guid? ?? Guid.Empty,
                            CredType = reader["CREDENTIAL_TYPE"] as string,
                            Descriptor = new PublicKeyCredentialDescriptor(reader["CREDENTIAL_ID"] as byte[]),
                            PublicKey = reader["PUBLIC_KEY"] as byte[],
                            RegDate = reader["REG_DATE"] as DateTime? ?? DateTime.MaxValue,
                            SignatureCounter = counter,
                            UserHandle = reader["USER_HANDLE"] as byte[],
                            UserId = reader["USER_ID"] as byte[]
                        };
                        credentials.Add(credential);
                    }
                }

                connection.Close();
            }

            return credentials;
        }

        public static void CreateUser(byte[] userId, string username, string displayName, string password,
            StoredCredential storedCredential, string connectionString)
        {
            var salt = GetSalt();
            var hash = GetHash(password, salt);

            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var command = connection.CreateCommand();

            command.CommandText = @"
                insert into USERS
                    (
                        USER_ID,
                        USERNAME,
                        DISPLAY_NAME,
                        USER_HANDLE,
                        PASSWORD_HASH,
                        PASSWORD_SALT,
                        CREDENTIAL_ID,
                        CREDENTIAL_GUID,
                        CREDENTIAL_TYPE,
                        PUBLIC_KEY,
                        REG_DATE,
                        SIGNATURE_COUNTER,
                        DESCRIPTOR_ID
                    )
                    values 
                    (
                        @USER_ID,
                        @USERNAME,
                        @DISPLAY_NAME,
                        @USER_HANDLE,
                        @PASSWORD_HASH,
                        @PASSWORD_SALT,
                        @CREDENTIAL_ID,
                        @CREDENTIAL_GUID,
                        @CREDENTIAL_TYPE,
                        @PUBLIC_KEY,
                        @REG_DATE,
                        @SIGNATURE_COUNTER,
                        @DESCRIPTOR_ID
                    )";

            command.Parameters.AddWithValue("@USER_ID", userId);
            command.Parameters.AddWithValue("@USERNAME", username);
            command.Parameters.AddWithValue("@DISPLAY_NAME", displayName);

            command.Parameters.AddWithValue("@PASSWORD_HASH", hash);
            command.Parameters.AddWithValue("@PASSWORD_SALT", salt);

            command.Parameters.AddWithValue("@CREDENTIAL_ID", storedCredential.Descriptor.Id);
            command.Parameters.AddWithValue("@CREDENTIAL_GUID", storedCredential.AaGuid);
            command.Parameters.AddWithValue("@CREDENTIAL_TYPE", storedCredential.CredType);
            command.Parameters.AddWithValue("@PUBLIC_KEY", storedCredential.PublicKey);
            command.Parameters.AddWithValue("@REG_DATE", storedCredential.RegDate);
            command.Parameters.AddWithValue("@SIGNATURE_COUNTER", (int) storedCredential.SignatureCounter);
            command.Parameters.AddWithValue("@USER_HANDLE", storedCredential.UserHandle);
            command.Parameters.AddWithValue("@DESCRIPTOR_ID", storedCredential.Descriptor.Id);
            command.ExecuteNonQuery();


            connection.Close();
        }

        public static StoredCredential GetCredentialById(byte[] credentialId, string connectionString)
        {
            StoredCredential credential = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select * from USERS where CREDENTIAL_ID = @CREDENTIAL_ID";
                    command.Parameters.AddWithValue("@credential_id", credentialId);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var counter = (uint) (reader["SIGNATURE_COUNTER"] as int? ?? 0);
                        credential = new StoredCredential
                        {
                            AaGuid = reader["CREDENTIAL_GUID"] as Guid? ?? Guid.Empty,
                            CredType = reader["CREDENTIAL_TYPE"] as string,
                            Descriptor = new PublicKeyCredentialDescriptor(reader["CREDENTIAL_ID"] as byte[]),
                            PublicKey = reader["PUBLIC_KEY"] as byte[],
                            RegDate = reader["REG_DATE"] as DateTime? ?? DateTime.MaxValue,
                            SignatureCounter = counter,
                            UserHandle = reader["USER_HANDLE"] as byte[],
                            UserId = Encoding.UTF8.GetBytes(reader["USERNAME"] as string)
                        };
                    }
                }

                connection.Close();
            }

            return credential;
        }

        public static List<StoredCredential> GetCredentialsByUserHandle(byte[] userHandle, string connectionString)
        {
            var storedCredentials = new List<StoredCredential>();
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "select * from CREDENTIALS where USER_HANDLE = @USER_HANDLE";
                    command.Parameters.AddWithValue("@USER_HANDLE", userHandle);
                    var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var counter = (uint) (reader["SIGNATURE_COUNTER"] as int? ?? 0);
                        var credential = new StoredCredential
                        {
                            AaGuid = reader["CREDENTIAL_GUID"] as Guid? ?? Guid.Empty,
                            CredType = reader["CREDENTIAL_TYPE"] as string,
                            Descriptor = new PublicKeyCredentialDescriptor(reader["CREDENTIAL_ID"] as byte[]),
                            PublicKey = reader["PUBLIC_KEY"] as byte[],
                            RegDate = reader["REG_DATE"] as DateTime? ?? DateTime.MaxValue,
                            SignatureCounter = counter,
                            UserHandle = reader["USER_HANDLE"] as byte[],
                            UserId = Encoding.UTF8.GetBytes(reader["USERNAME"] as string)
                        };
                        storedCredentials.Add(credential);
                    }
                }

                connection.Close();
            }

            return storedCredentials;
        }

        public static void UpdateCounter(byte[] credentialId, uint resCounter, string connectionString)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText =
                        "update CREDENTIALS set SIGNATURE_COUNTER = @counter WHERE CREDENTIAL_ID = @CREDENTIAL_ID";
                    command.Parameters.AddWithValue("@counter", (int) resCounter);
                    command.Parameters.AddWithValue("@credential_id", credentialId);
                    command.ExecuteNonQuery();
                }

                connection.Close();
            }
        }

        public static string FormatException(Exception e)
        {
            return $"{e.Message}{(e.InnerException != null ? " (" + e.InnerException.Message + ")" : "")}";
        }

        public static User GetUser(string username, string connectionString)
        {
            User user = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "select * from USERS where username = @username";
                    cmd.Parameters.AddWithValue("@username", username);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        user = new User
                        {
                            Id = reader["USER_ID"] as byte[],
                            DisplayName = reader["DISPLAY_NAME"] as string,
                            Name = reader["USERNAME"] as string,
                            PasswordHash = reader["PASSWORD_HASH"] as byte[],
                            PasswordSalt = reader["PASSWORD_SALT"] as byte[]
                        };
                    reader.Close();
                }

                connection.Close();
            }

            return user;
        }

        //public static User GetUserById(byte[] userId, string connectionString)
        //{
        //    User user = null;
        //    using (var connection = new SqlConnection(connectionString))
        //    {
        //        connection.Open();
        //        using (var cmd = connection.CreateCommand())
        //        {
        //            cmd.CommandText =
        //                "select * from USERS where user_id = @user_id";
        //            cmd.Parameters.AddWithValue("@user_id", userId);
        //            var reader = cmd.ExecuteReader();
        //            while (reader.Read())
        //                user = new User
        //                {
        //                    Id = reader["USER_ID"] as byte[],
        //                    DisplayName = reader["DISPLAY_NAME"] as string,
        //                    Name = reader["USERNAME"] as string,
        //                    PasswordHash = reader["PASSWORD_HASH"] as byte[],
        //                    PasswordSalt = reader["PASSWORD_SALT"] as byte[]
        //                };
        //            reader.Close();
        //        }

        //        connection.Close();
        //    }

        //    return user;
        //}

        public static User GetUserByCredentialId(byte[] credentialId, string connectionString)
        {
            User user = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "select * from USERS where credential_id = @credential_id";
                    cmd.Parameters.AddWithValue("@credential_id", credentialId);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        user = new User
                        {
                            Id = reader["USER_ID"] as byte[],
                            DisplayName = reader["DISPLAY_NAME"] as string,
                            Name = reader["USERNAME"] as string,
                            PasswordHash = reader["PASSWORD_HASH"] as byte[],
                            PasswordSalt = reader["PASSWORD_SALT"] as byte[]
                        };
                    reader.Close();
                }

                connection.Close();
            }

            return user;
        }

        public static User GetUserByUsername(string username, string connectionString)
        {
            User user = null;
            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText =
                        "select * from USERS where username = @username";
                    cmd.Parameters.AddWithValue("@username", username);
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                        user = new User
                        {
                            Id = reader["USER_ID"] as byte[],
                            DisplayName = reader["DISPLAY_NAME"] as string,
                            Name = reader["USERNAME"] as string,
                            PasswordHash = reader["PASSWORD_HASH"] as byte[],
                            PasswordSalt = reader["PASSWORD_SALT"] as byte[]
                        };
                    reader.Close();
                }

                connection.Close();
            }

            return user;
        }
    }
}