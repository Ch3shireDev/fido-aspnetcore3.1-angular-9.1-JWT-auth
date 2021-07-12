namespace WebAPI.Services
{
    public interface IUserService
    {
        User GetOrAddUser(string username, string password, string displayName);
        User GetUser(string username);
        User GetById(int id);
    }
}