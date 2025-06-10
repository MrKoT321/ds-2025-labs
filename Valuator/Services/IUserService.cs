namespace Valuator.Services;

public interface IUserService
{
    Task<User.User?> FindByUsernameAsync(string username);
    Task<User.User> CreateUserAsync(string username, string password);
    bool VerifyPassword(User.User user, string password);
}