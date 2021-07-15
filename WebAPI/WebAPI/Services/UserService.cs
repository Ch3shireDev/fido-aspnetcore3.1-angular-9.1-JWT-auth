using System.Collections.Generic;
using Fido2NetLib;
using Fido2NetLib.Development;
using Microsoft.Extensions.Options;
using WebAPI.Helpers;

namespace WebAPI.Services
{
    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;

        public UserService(IOptions<AppSettings> appSettings)
        {
            _appSettings = appSettings.Value;
        }

        private string connectionString => "Server=HP-ZBOOK-IGOR;Database=test;Trusted_Connection=True;";

        public User GetOrAddUser(string username, string password, string displayName)
        {
            var user = Tools.GetUser(username, connectionString) ??
                       Tools.CreateUser(username, password, displayName, connectionString);
            return user;
        }

        public User GetUser(string username)
        {
            var user = Tools.GetUser(username, connectionString);
            return user;
        }


        public User GetById(int id)
        {
            return Tools.GetUserById(id, connectionString);
        }

        public void AddCredentialToUser(Fido2User optionsUser, StoredCredential storedCredential)
        {
            Tools.AddCredentialToUser(optionsUser.Name, storedCredential, connectionString);
        }

        public List<Fido2User> GetUsersByCredentialId(byte[] credentialId)
        {
            return Tools.GetUsersByCredentialId(credentialId, connectionString);
        }

        public List<StoredCredential> GetCredentialsByUser(User user)
        {
            return Tools.GetCredentialsByUser(user.Username, connectionString);
        }

        public StoredCredential GetCredentialById(byte[] credentialId)
        {
            return Tools.GetCredentialById(credentialId, connectionString);
        }

        public List<StoredCredential> GetCredentialsByUserHandle(byte[] userHandle)
        {
            return Tools.GetCredentialsByUserHandle(userHandle, connectionString);
        }

        public void UpdateCounter(byte[] credentialId, uint resCounter)
        {
            Tools.UpdateCounter(credentialId, resCounter, connectionString);
        }
    }
}