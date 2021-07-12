
namespace WebAPI.Services
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string UserId { get; set; }
        public string Token { get; set; }
    }
}
