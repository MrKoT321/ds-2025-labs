namespace RankCalculator.Services;

public interface IRedisService
{
    Task<string> GetTextAsyncByShardKey(string id, string shardKey);
    Task SetRankAsyncSharded(string id, double rank, string shardKey);
}