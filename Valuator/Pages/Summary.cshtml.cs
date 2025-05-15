using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
        Rank = "Загрузка данных...";
        Similarity = "Загрузка данных...";
    }

    public string Rank { get; set; }
    public string Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);
        IDatabase db = _redis.GetDatabase();

        string rankKey = "RANK-" + id;
        if (double.TryParse(db.StringGet(rankKey), out double rankValue))
        {
            Rank = rankValue.ToString();
        }
        string similarityKey = "SIMILARITY-" + id;
        string similarityStr = db.StringGet(similarityKey);
        Similarity = (similarityStr.Equals("True", StringComparison.OrdinalIgnoreCase) ? 1 : 0).ToString();
    }
}
