using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private const string NotCompleteAssessment = "Оценка содержания не завершена";
    private const int MaxAttempts = 5;
    private readonly ILogger<SummaryModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
        Rank = NotCompleteAssessment;
        Similarity = NotCompleteAssessment;
    }

    public string Rank { get; set; }
    public string Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);
        IDatabase db = _redis.GetDatabase();

        int attemptsCount = 0;
        string rankKey = "RANK-" + id;

        while (attemptsCount < MaxAttempts || !db.KeyExists(rankKey))
        {
            string? rankValue = db.StringGet(rankKey);
            if (!string.IsNullOrEmpty(rankValue))
            {
                Rank = rankValue;
            }

            attemptsCount++;
        }

        string similarityKey = "SIMILARITY-" + id;
        string? similarityStr = db.StringGet(similarityKey);
        
        Similarity = string.IsNullOrEmpty(similarityStr)
            ? "Не удалось получить результат"
            : similarityStr.Equals("True", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
    }
}