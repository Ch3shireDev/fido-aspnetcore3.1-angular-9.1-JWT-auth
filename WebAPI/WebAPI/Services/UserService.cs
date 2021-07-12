using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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

        private string ConnectionString => _appSettings.ConnectionString;

        public User GetOrAddUser(string username, string password, string displayName)
        {
            var user = Tools.GetUser(username, ConnectionString) ??
                       Tools.CreateUser(username, password, displayName, ConnectionString);
            return user;
        }

        public User GetUser(string username)
        {
            var user = Tools.GetUser(username, ConnectionString);
            if (user == null) return null;
            return user;
        }


        public User GetById(int id)
        {
            return Tools.GetUserById(id, ConnectionString);
        }
    }
}