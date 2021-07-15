using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Fido2NetLib;
using Fido2NetLib.Development;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebAPI.Helpers;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [Route("/api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly IFido2 _fido2;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public UserController(IUserService userService, IMapper mapper, IFido2 fido2,
            IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _fido2 = fido2;
        }

        [HttpPost("register-begin")]
        public async Task<ActionResult<User>> RegisterBegin([FromForm] string username, [FromForm] string password,
            [FromForm] string displayName)
        {
            var user = _mapper.Map<UserModel>(_userService.GetOrAddUser(username, password, displayName));

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

            var attType = "none";

            var requireResidentKey = false;
            var userVerification = "preferred";

            var authenticatorSelection = new AuthenticatorSelection
            {
                RequireResidentKey = requireResidentKey,
                UserVerification = userVerification.ToEnum<UserVerificationRequirement>()
            };

            var attestation = attType.ToEnum<AttestationConveyancePreference>();

            var options = _fido2.RequestNewCredential(fido2user, existingKeys, authenticatorSelection,
                attestation, exts);

            HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());


            return new JsonResult(options);
        }

        [HttpPost("register-end")]
        public async Task<ActionResult> RegisterEnd([FromBody] AuthenticatorAttestationRawResponse attestationResponse)
        {
            async Task<bool> CallbackAsync(IsCredentialIdUniqueToUserParams args)
            {
                var users = _userService.GetUsersByCredentialId(args.CredentialId);
                return users.Count == 0;
            }

            try
            {
                // 1. get the options we sent the client
                var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
                var options = CredentialCreateOptions.FromJson(jsonOptions);

                // 2. Create callback so that lib can verify credential id is unique to this user

                // 2. Verify and make the credentials
                var success = await _fido2.MakeNewCredentialAsync(attestationResponse, options, CallbackAsync);

                // 3. Store the credentials in db
                _userService.AddCredentialToUser(options.User, new StoredCredential
                {
                    Descriptor = new PublicKeyCredentialDescriptor(success.Result.CredentialId),
                    PublicKey = success.Result.PublicKey,
                    UserHandle = success.Result.User.Id,
                    SignatureCounter = success.Result.Counter,
                    CredType = success.Result.CredType,
                    RegDate = DateTime.Now,
                    AaGuid = success.Result.Aaguid
                });

                return new JsonResult(success);

                // 4. return "ok" to the client
                //return new JsonResult(new
                //    {
                //        result = new
                //        {
                //            publicKey =
                //                "pQECAyYgASFYIFVfGAShwyCaLxf-zA9SGG6aD9zpsIdoyP2lIccb32RaIlggQHxLdtX8NtlhVSioMfy8ixpAKPEFQVdUIyKyKjWuqyE",
                //            user = new {name = "alice", id = "YWxpY2U", displayName = "alice"},
                //            credType = "fido-u2f",
                //            aaguid = "00000000-0000-0000-0000-000000000000",
                //            credentialId =
                //                "USyMIGB27Jaf5CdKhAmrvST81KnLxETcpRvTZXSwsEhTXhLZZeFTHTJ9pzBnlmuUFVZfnK7wxtmzHvwM/IsEuw==",
                //            counter = 0,
                //            status = (string) null,
                //            errorMessage = (string) null
                //        },
                //        status = "ok",
                //        errorMessage = ""
                //    }
                //);
            }
            catch (Exception e)
            {
                return new JsonResult(new Fido2.CredentialMakeResult
                    {Status = "error", ErrorMessage = FormatException(e)});
            }

            return StatusCode(200);
        }

        [AllowAnonymous]
        [HttpPost("login-begin")]
        public ActionResult LoginBegin([FromForm] string username, [FromForm] string userVerification)
        {
            try
            {
                var existingCredentials = new List<PublicKeyCredentialDescriptor>();

                if (!string.IsNullOrEmpty(username))
                {
                    // 1. Get user from DB
                    //var user = DemoStorage.GetUser(username);
                    var user = _userService.GetUser(username);
                    if (user == null) throw new ArgumentException("Username was not registered");

                    // 2. Get registered credentials from database
                    existingCredentials = _userService.GetCredentialsByUser(user).Select(c => c.Descriptor).ToList();
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

                // 4. Temporarily store options, session/in-memory cache/redis/db
                HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());

                // 5. Return options to client
                return new JsonResult(options);


                //var user = _userService.GetUser(username);
                //if (user == null) return BadRequest();
                //var tokenHandler = new JwtSecurityTokenHandler();
                //var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                //var tokenDescriptor = new SecurityTokenDescriptor
                //{
                //    Subject = new ClaimsIdentity(new[]
                //    {
                //        new Claim(ClaimTypes.Name, user.Id.ToString())
                //    }),
                //    Expires = DateTime.UtcNow.AddDays(7),
                //    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                //        SecurityAlgorithms.HmacSha256Signature)
                //};
                //var securityToken = tokenHandler.CreateToken(tokenDescriptor);
                //user.Token = tokenHandler.WriteToken(securityToken);
                //return Ok(new { user });

                return new JsonResult(
                    new

                    {
                        challenge = "n8rn7ian4-gaobjHCDj92Q",
                        timeout = 60000,
                        rpId = "localhost",
                        allowCredentials = new[]
                        {
                            new
                            {
                                type = "public-key",
                                id =
                                    "0CIB6Qz7aM-_2mkEUgqKCszWMLrOu16lVm2hSCIxpxVojg0bz4XUXKEakOw-XJmNbU9ctWTYBwFwD5ii5vdpxg"
                            }
                        },
                        userVerification = "discouraged",
                        extensions = new
                        {
                            txAuthSimple = "FIDO",
                            txAuthGenericArg = new {contentType = "text/plain", content = "RklETw=="},
                            uvi = true, loc = true, uvm = true
                        },
                        status = "ok",
                        errorMessage = ""
                    }
                );
            }

            catch (Exception e)
            {
                return new JsonResult(new AssertionOptions {Status = "error", ErrorMessage = FormatException(e)});
            }
        }

        [HttpPost("login-end")]
        public async Task<ActionResult> LoginEnd([FromBody] AuthenticatorAssertionRawResponse clientResponse)
        {
            try
            {
                // 1. Get the assertion options we sent the client
                var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");
                var options = AssertionOptions.FromJson(jsonOptions);

                // 2. Get registered credential from database
                //var creds = DemoStorage.GetCredentialById(clientResponse.Id);
                var creds = _userService.GetCredentialById(clientResponse.Id);

                if (creds == null) throw new Exception("Unknown credentials");

                // 3. Get credential counter from database
                var storedCounter = creds.SignatureCounter;

                // 4. Create callback to check if userhandle owns the credentialId
                IsUserHandleOwnerOfCredentialIdAsync callback = async args =>
                {
                    var storedCreds = _userService.GetCredentialsByUserHandle(args.UserHandle);
                    return storedCreds.Exists(c => c.Descriptor.Id.SequenceEqual(args.CredentialId));
                };

                // 5. Make the assertion
                var res = await _fido2.MakeAssertionAsync(clientResponse, options, creds.PublicKey, storedCounter,
                    callback);

                // 6. Store the updated counter
                _userService.UpdateCounter(res.CredentialId, res.Counter);

                // 7. return OK to client
                return new JsonResult(res);
            }
            catch (Exception e)
            {
                return new JsonResult(new AssertionVerificationResult
                    {Status = "error", ErrorMessage = FormatException(e)});
            }
        }

        private string FormatException(Exception e)
        {
            return $"{e.Message}{(e.InnerException != null ? " (" + e.InnerException.Message + ")" : "")}";
        }
    }
}