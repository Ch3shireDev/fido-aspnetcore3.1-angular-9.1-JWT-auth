using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;

namespace WebAPI.Services
{
    public interface IUserService
    {
        //User GetOrAddUser(string username, string password, string displayName);
        //User GetUser(string username);
        //void CreateUser(Fido2User user, StoredCredential credentials);
        //List<Fido2User> GetUsersByCredentialId(byte[] credentialId);
        //List<StoredCredential> GetCredentialsByUser(User user);
        //StoredCredential GetCredentialById(byte[] credentialId);
        //List<StoredCredential> GetCredentialsByUserHandle(byte[] userHandle);
        //void UpdateCounter(byte[] credentialId, uint resCounter);
        //User GetById(int id);

        CredentialCreateOptions RegisterBegin(string username, string displayName, string password,
            AttestationConveyancePreference attestation, in bool requireResidentKey,
            UserVerificationRequirement userVerificationRequirement);

        Task<Fido2.CredentialMakeResult> RegisterEnd(AuthenticatorAttestationRawResponse attestationResponse,
            string password, string jsonOptions);

        AssertionOptions LoginBegin(string username, string password, string userVerification);

        Task<AssertionVerificationResult> LoginEnd(AuthenticatorAssertionRawResponse clientResponse,
            string jsonOptions);

        Fido2User GetByUserId(byte[] userId);
        User GetByCredentialId(byte[] credentialId);
        User GetByUsername(string username);
    }
}