using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WebAPI.Helpers;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [Route("/api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;
        private readonly IFido2 _fido2;

        public UserController(IUserService userService, IMapper mapper, IFido2 fido2,
            IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
            _fido2 = fido2;
        }

        [HttpPost("register-begin")]
        public ActionResult<User> RegisterBegin([FromForm] string username, [FromForm] string displayName,
            [FromForm] string password1, [FromForm] string password2)
        {
            if (password1 != password2) return BadRequest();
            var password = password1;
            var user = _mapper.Map<UserModel>(_userService.GetOrAddUser(username, password, displayName));

            var exts = new AuthenticationExtensionsClientInputs()
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
            var options = _fido2.RequestNewCredential(fido2user, existingKeys, authenticatorSelection, attType.ToEnum<AttestationConveyancePreference>(), exts);

            return new JsonResult(options);
        }

        [HttpPost("register-end")]
        public ActionResult RegisterEnd()
        {
            return StatusCode(200);
        }

        [AllowAnonymous]
        [HttpPost("login-begin")]
        public ActionResult LoginBegin([FromForm] string username, [FromForm] string password)
        {
            var user = _userService.GetUser(username);
            if (user == null) return BadRequest();
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            user.Token = tokenHandler.WriteToken(securityToken);
            return Ok(new {user});
        }

        [HttpPost("login-end")]
        public ActionResult LoginEnd()
        {
            return StatusCode(200);
        }
    }
}