namespace RankCalculator.Services;

using StackExchange.Redis;

public class RedisService : IRedisService
{
    public async Task<string> GetTextAsyncByShardKey(string id, string key)
    {
        IDatabase database = ConnectToDatabase(key);
        Console.WriteLine($"LOOKUP: {id}, {key}");

        var result = await database.StringGetAsync("TEXT-" + id);
        
        return result.ToString();
    }

    public async Task SetRankAsyncSharded(string id, double rank, string shardKey)
    {
        IDatabase database = ConnectToDatabase(shardKey);
        await database.StringSetAsync("RANK-" + id, rank.ToString());
        Console.WriteLine($"LOOKUP: {id}, {shardKey}");
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
