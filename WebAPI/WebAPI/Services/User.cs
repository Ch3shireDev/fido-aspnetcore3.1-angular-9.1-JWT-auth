using Fido2NetLib;

namespace WebAPI.Services
{
    public class User : Fido2User
    {
        //public int Id { get; set; }
        //public string Username { get; set; }
        //public string DisplayName { get; set; }
        public byte[] PasswordHash { get; set; }

        public byte[] PasswordSalt { get; set; }
        //public string Token { get; set; }
    }
}