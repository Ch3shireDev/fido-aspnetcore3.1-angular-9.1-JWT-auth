using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
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
        public ActionResult<User> RegisterBegin([FromForm] string username, [FromForm] string password,
            [FromForm] string displayName)
        {
            try
            {
                var attType = "none";
                var userVerification = "preferred";

                var options = _userService.RegisterBegin(username, displayName, password,
                    attType.ToEnum<AttestationConveyancePreference>(), false,
                    userVerification.ToEnum<UserVerificationRequirement>());

                HttpContext.Session.SetString("fido2.attestationOptions", options.ToJson());

                return new JsonResult(options);
            }
            catch (Exception e)
            {
                return new JsonResult(new Fido2.CredentialMakeResult
                    {Status = "error", ErrorMessage = Tools.FormatException(e)});
            }
        }

        [HttpPost("register-end")]
        public async Task<ActionResult> RegisterEnd([FromForm] string attestationResponse, [FromForm] string password)
        {
            try
            {
                var attestationResponseJson =
                    JsonConvert.DeserializeObject<AuthenticatorAttestationRawResponse>(attestationResponse);
                var jsonOptions = HttpContext.Session.GetString("fido2.attestationOptions");
                var success = await _userService.RegisterEnd(attestationResponseJson, password, jsonOptions);
                return new JsonResult(success);
            }
            catch (Exception e)
            {
                return new JsonResult(new Fido2.CredentialMakeResult
                    {Status = "error", ErrorMessage = Tools.FormatException(e)});
            }
        }

        [AllowAnonymous]
        [HttpPost("login-begin")]
        public ActionResult LoginBegin([FromForm] string username, [FromForm] string password,
            [FromForm] string userVerification)
        {
            try
            {
                var options = _userService.LoginBegin(username, password, userVerification);

                // 4. Temporarily store options, session/in-memory cache/redis/db
                HttpContext.Session.SetString("fido2.assertionOptions", options.ToJson());

                // 5. Return options to client
                return new JsonResult(options);
            }

            catch (Exception e)
            {
                return new JsonResult(new AssertionOptions {Status = "error", ErrorMessage = Tools.FormatException(e)});
            }
        }

        [HttpPost("login-end")]
        public async Task<ActionResult> LoginEnd([FromBody] AuthenticatorAssertionRawResponse clientResponse)
        {
            try
            {
                // 1. Get the assertion options we sent the client
                var jsonOptions = HttpContext.Session.GetString("fido2.assertionOptions");

                var response = await _userService.LoginEnd(clientResponse, jsonOptions);
                var user = _userService.GetByCredentialId(response.CredentialId);

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.Name, user.Name)
                    }),
                    Expires = DateTime.UtcNow.AddDays(7),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                        SecurityAlgorithms.HmacSha256Signature)
                };
                var tokenData = tokenHandler.CreateToken(tokenDescriptor);
                var token = tokenHandler.WriteToken(tokenData);

                // 7. return OK to client
                return new JsonResult(new
                {
                    Status = "ok",
                    ErrorMessage = (string) null,
                    user.DisplayName,
                    token,
                    response
                });
            }
            catch (Exception e)
            {
                return new JsonResult(new AssertionVerificationResult
                    {Status = "error", ErrorMessage = Tools.FormatException(e)});
            }
        }
    }
}