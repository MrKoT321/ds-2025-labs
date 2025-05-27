namespace RankCalculator.Services;

public interface IRedisService
{
    Task<string> GetTextAsync(string id);
    Task SetRankAsync(string id, double rank);
    Task<double> GetRankAsync(string id);
}