using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using System.Text;
using Valuator.Calculator;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        _logger.LogDebug(text);
        
        string id = TextToHash(text);
        IDatabase db = _redis.GetDatabase();

        string rankKey = "RANK-" + id;
        db.StringSet(rankKey, Math.Round(ValuatorCalculator.CalculateRank(text), 2).ToString());

        ValuatorCalculator calculator = new ValuatorCalculator(_redis);
        string similarityKey = "SIMILARITY-" + id;
        db.StringSet(similarityKey, calculator.CheckSimilarity(text).ToString());
        
        return Redirect($"summary?id={id}");
    }
    
    private string TextToHash(string text)
    {
        SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));

        var builder = new StringBuilder();
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2")); 
        }

        return builder.ToString();
    }
}
