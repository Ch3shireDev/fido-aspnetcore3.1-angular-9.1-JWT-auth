using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
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
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppSettings _appSettings;
        private readonly IMapper _mapper;
        private readonly IUserService _userService;

        public UserController(IUserService userService, IMapper mapper,
            IOptions<AppSettings> appSettings)
        {
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSettings.Value;
        }

        [HttpPost("register-begin")]
        public ActionResult<User> RegisterBegin([FromForm] string username, [FromForm] string displayName,
            [FromForm] string password1, [FromForm] string password2)
        {
            if (password1 != password2) return BadRequest();
            var password = password1;
            var user = _mapper.Map<UserModel>(_userService.GetOrAddUser(username, password, displayName));
            return Ok(user);
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