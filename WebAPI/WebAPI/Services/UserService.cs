using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Options;
using WebAPI.Helpers;

namespace WebAPI.Services
{
    public class UserService : IUserService
    {
        private readonly AppSettings _appSettings;
        private readonly IFido2 _fido2;

        public UserService(IOptions<AppSettings> appSettings, IFido2 fido2)
        {
            _appSettings = appSettings.Value;
            _fido2 = fido2;
        }

        private string connectionString => _appSettings.ConnectionString;

        //public User GetById(int id)
        //{
        //    return Tools.GetUserById(id, connectionString);
        //}

        public CredentialCreateOptions RegisterBegin(string username, string displayName, string password,
            AttestationConveyancePreference attestation,
            in bool requireResidentKey, UserVerificationRequirement userVerification)
        {
            var user = GetUser(username);
            if (user != null) throw new Exception($"User {username} exists!");

            var exts = new AuthenticationExtensionsClientInputs
            {
                Extensions = true,
                UserVerificationIndex = true,
                Location = true,
                UserVerificationMethod = true,
                BiometricAuthenticatorPerformanceBounds = new AuthenticatorBiometricPerfBounds
                {
                    FAR = float.MaxValue,
                    FRR = float.MaxValue
                }
            };
            var existingKeys = new List<PublicKeyCredentialDescriptor>();

            var fido2user = new Fido2User
            {
                DisplayName = displayName,
                Id = Encoding.UTF8.GetBytes(username),
                Name = username
            };

            var authenticatorSelection = new AuthenticatorSelection
            {
                RequireResidentKey = requireResidentKey,
                UserVerification = userVerification
            };

            var options = _fido2.RequestNewCredential(fido2user, existingKeys, authenticatorSelection,
                attestation, exts);

            return options;
        }

        public async Task<Fido2.CredentialMakeResult> RegisterEnd(
            AuthenticatorAttestationRawResponse attestationResponse, string password, string jsonOptions)
        {
            async Task<bool> CallbackAsync(IsCredentialIdUniqueToUserParams args)
            {
                var users = GetUsersByCredentialId(args.CredentialId);
                return users.Count == 0;
            }

            // 1. get the options we sent the client
            var options = CredentialCreateOptions.FromJson(jsonOptions);

            // 2. Create callback so that lib can verify credential id is unique to this user

            // 2. Verify and make the credentials
            var success = await _fido2.MakeNewCredentialAsync(attestationResponse, options, CallbackAsync);

            // 3. Store the credentials in db
            CreateUser(options.User, password, new StoredCredential
            {
                Descriptor = new PublicKeyCredentialDescriptor(success.Result.CredentialId),
                PublicKey = success.Result.PublicKey,
                UserHandle = success.Result.User.Id,
                SignatureCounter = success.Result.Counter,
                CredType = success.Result.CredType,
                RegDate = DateTime.Now,
                AaGuid = success.Result.Aaguid
            });

            return success;
        }

        public AssertionOptions LoginBegin(string username, string password, string userVerification)
        {
            var existingCredentials = new List<PublicKeyCredentialDescriptor>();

            if (!string.IsNullOrEmpty(username))
            {
                // 1. Get user from DB
                //var user = DemoStorage.GetUser(username);
                var user = GetUser(username);
                if (user == null) throw new ArgumentException("Username was not registered");

                var passwordHash = Tools.GetHash(password, user.PasswordSalt);
                var hash1 = Convert.ToBase64String(passwordHash);
                var hash2 = Convert.ToBase64String(user.PasswordHash);
                if (hash1 != hash2)
                    throw new ArgumentException($"Niepoprawne hasło dla użytkownika {username}.");

                // 2. Get registered credentials from database
                existingCredentials = GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();
            }

            var exts = new AuthenticationExtensionsClientInputs
            {
                SimpleTransactionAuthorization = "FIDO",
                GenericTransactionAuthorization = new TxAuthGenericArg
                {
                    ContentType = "text/plain",
                    Content = new byte[] {0x46, 0x49, 0x44, 0x4F}
                },
                UserVerificationIndex = true,
                Location = true,
                UserVerificationMethod = true
            };

            // 3. Create options
            var uv = string.IsNullOrEmpty(userVerification)
                ? UserVerificationRequirement.Discouraged
                : userVerification.ToEnum<UserVerificationRequirement>();
            var options = _fido2.GetAssertionOptions(
                existingCredentials,
                uv,
                exts
            );

            return options;
        }

        public async Task<AssertionVerificationResult> LoginEnd(AuthenticatorAssertionRawResponse clientResponse,
            string jsonOptions)
        {
            var options = AssertionOptions.FromJson(jsonOptions);

            // 2. Get registered credential from database
            var creds = GetCredentialById(clientResponse.Id);

            if (creds == null) throw new Exception("Unknown credentials");

            // 3. Get credential counter from database
            var storedCounter = creds.SignatureCounter;

            //  async Task<bool> CallbackAsync(IsCredentialIdUniqueToUserParams args)

            // 4. Create callback to check if userhandle owns the credentialId
            async Task<bool> Callback(IsUserHandleOwnerOfCredentialIdParams args)
            {
                var storedCreds = GetCredentialsByUserHandle(args.UserHandle);
                return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
            }

            // 5. Make the assertion
            var res = await _fido2.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter,
                Callback);


            // 6. Store the updated counter
            UpdateCounter(res.CredentialId, res.Counter);

            return res;
        }

        public Fido2User GetByUserId(byte[] userId)
        {
            return Tools.GetUserById(userId, connectionString);
        }

        public User GetByCredentialId(byte[] credentialId)
        {
            return Tools.GetUserByCredentialId(credentialId, connectionString);
        }

        public User GetByUsername(string username)
        {
            return Tools.GetUserByUsername(username, connectionString);
        }

        public User GetUser(string username)
        {
            return Tools.GetUser(username, connectionString);
        }

        public void CreateUser(Fido2User user, string password, StoredCredential credentials)
        {
            Tools.CreateUser(user.Id, user.Name, user.DisplayName, password, credentials, connectionString);
        }

        public List<Fido2User> GetUsersByCredentialId(byte[] credentialId)
        {
            return Tools.GetUsersByCredentialId(credentialId, connectionString);
        }

        public List<StoredCredential> GetCredentialsByUser(Fido2User user)
        {
            return Tools.GetCredentialsByUser(user.Name, connectionString);
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