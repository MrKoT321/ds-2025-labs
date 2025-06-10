using StackExchange.Redis;

namespace Valuator.Services;

public class RedisStorageService : IStorageService
{
    public void SaveShardKey(string id, string shardKey)
    {
        IDatabase database = ConnectToDatabase("MAIN");
        database.StringSet(id, shardKey.ToUpper());

        Console.WriteLine($"LOOKUP: {id}, {shardKey}");
    }

    public void SaveById(string key, string value, string id)
    {
        string shardKey = GetShardKey(id);
        IDatabase database = ConnectToDatabase(shardKey);
        database.StringSet(key, value);

        Console.WriteLine($"LOOKUP: {id}, {shardKey}");
    }

    public void SaveByShardKey(string key, string value, string shardKey)
    {
        IDatabase database = ConnectToDatabase(shardKey);
        database.StringSet(key, value);

        string id = key.Substring(key.IndexOf('-') + 1);
        Console.WriteLine($"LOOKUP: {id}, {shardKey}");
    }

    public bool SaveContainsById(string key, string value, string id)
    {
        string shardKey = GetShardKey(id);
        IDatabase database = ConnectToDatabase(shardKey);

        Console.WriteLine($"LOOKUP: {id}, {shardKey}");

        bool isDuplicate = database.SetContains(key, value);
        if (isDuplicate)
        {
            return true;
        }

        database.SetAdd(key, value);
        return false;
    }

    public string? GetById(string id, string key)
    {
        string shardKey = GetShardKey(id);
        IDatabase database = ConnectToDatabase(shardKey);
        Console.WriteLine($"LOOKUP: {id}, {shardKey}");

        return database.StringGet(key);
    }

    public string? GetValue(string id, string shardKey)
    {
        var database = ConnectToDatabase(shardKey);
        return database.StringGet(id);
    }

    public string? GetUserIdByTextId(string id)
    {
        try
        {
            var shardKey = GetShardKey(id);
            return GetValue("USER-" + id, shardKey);
        }
        catch
        {
            return null;
        }
    }

    private string GetShardKey(string id)
    {
        IDatabase database = ConnectToDatabase("MAIN");

        var shardKey = database.StringGet(id);
        if (string.IsNullOrEmpty(shardKey))
        {
            throw new KeyNotFoundException("Shard key not found");
        }

        return shardKey!;
    }

    private IDatabase ConnectToDatabase(string shardKey)
    {
        string connectionString = GetEnvironmentString($"DB_{shardKey.ToUpper()}");
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