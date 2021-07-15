using System.Collections.Generic;
using Fido2NetLib;
using Fido2NetLib.Development;

namespace WebAPI.Services
{
    public interface IUserService
    {
        User GetOrAddUser(string username, string password, string displayName);
        User GetUser(string username);
        User GetById(int id);
        void AddCredentialToUser(Fido2User optionsUser, StoredCredential storedCredential);
        List<Fido2User> GetUsersByCredentialId(byte[] credentialId);
        List<StoredCredential> GetCredentialsByUser(User user);
        StoredCredential GetCredentialById(byte[] credentialId);
        List<StoredCredential> GetCredentialsByUserHandle(byte[] userHandle);
        void UpdateCounter(byte[] credentialId, uint resCounter);
    }
}