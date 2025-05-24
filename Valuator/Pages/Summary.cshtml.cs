using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    public const string NotCompleteAssessment = "в процессе...";
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
    public string Id { get; private set; }

    public void OnGet(string id)
    {
        Id = id;

        _logger.LogDebug(id);
        IDatabase db = _redis.GetDatabase();

        string similarityKey = "SIMILARITY-" + id;
        string? similarityStr = db.StringGet(similarityKey);
        
        string rankKey = "RANK-" + id;
        string? rankValue = db.StringGet(rankKey);

        if (!string.IsNullOrEmpty(similarityStr))
        {
            Similarity = similarityStr.Equals("True", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
        }

        if (!string.IsNullOrEmpty(rankValue))
        {
            Rank = rankValue;
        }
    }
}