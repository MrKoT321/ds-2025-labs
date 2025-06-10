using Microsoft.AspNetCore.Identity;
using StackExchange.Redis;

namespace Valuator.Services;

public class UserService : IUserService
{
    private readonly IDatabase _database;
    private readonly IPasswordHasher<User.User> _passwordHasher;

    public UserService()
    {
        _database = ConnectToDatabase("USER");;
        _passwordHasher = new PasswordHasher<User.User>();
    }

    public async Task<User.User?> FindByUsernameAsync(string username)
    {
        var hashEntries = await _database.HashGetAllAsync(username);
        if (hashEntries.Length == 0)
        {
            return null;
        }

        var idValue = hashEntries.FirstOrDefault(x => x.Name == "Id").Value;
        if (!Guid.TryParse(idValue, out var parsedId))
        {
            return null;
        }
        
        return new User.User
        {
            Username = username,
            Id = parsedId,
            Password = hashEntries.FirstOrDefault(x => x.Name == "Password").Value!
        };
    }

    public async Task<User.User> CreateUserAsync(string username, string password)
    {
        var user = new User.User
        {
            Id = Guid.NewGuid(),
            Username = username
        };

        user.Password = _passwordHasher.HashPassword(user, password);
        
        var hashEntries = new HashEntry[]
        {
            new("Id", user.Id.ToString()),
            new("Password", user.Password)
        };

        await _database.HashSetAsync(user.Username, hashEntries);
        return user;
    }

    public bool VerifyPassword(User.User user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, user.Password, password);
        return result == PasswordVerificationResult.Success;
    }
    
    private IDatabase ConnectToDatabase(string key)
    {
        string connectionString = GetEnvironmentString($"DB_{key.ToUpper()}");
        string password = GetEnvironmentString("REDIS_PASSWORD");
        
        var options = ConfigurationOptions.Parse(connectionString);
        options.Password = password;
        
        IConnectionMultiplexer redis = ConnectionMultiplexer.Connect(options);

        return redis.GetDatabase();
    }
    
    private static string GetEnvironmentString(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrEmpty(value))
        {
            throw new Exception($"Env string was not found {key}");
        }

        return value;
    }
}