namespace RankCalculator.Services;

using StackExchange.Redis;

public class RedisService(ConnectionMultiplexer redis) : IRedisService
{
    private readonly IDatabase _database = redis.GetDatabase();

    public async Task<string> GetTextAsync(string id)
    {
        RedisValue value = await _database.StringGetAsync("TEXT-" + id);
        return value.ToString();
    }

    public async Task SetRankAsync(string id, double rank)
    {
        await _database.StringSetAsync("RANK-" + id, rank.ToString());
    }

    public async Task<double> GetRankAsync(string id)
    {
        double.TryParse(await _database.StringGetAsync("RANK-" + id), out var rank);
        return rank;
    }
}
