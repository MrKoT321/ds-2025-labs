using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using System.Text;
using Valuator.Calculator;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IRabbitMqService _rabbitMqService;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis, IRabbitMqService rabbitMqService)
    {
        _logger = logger;
        _redis = redis;
        _rabbitMqService = rabbitMqService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string text)
    {
        _logger.LogDebug(text);

        string id = TextToHash(text);
        IDatabase db = _redis.GetDatabase();

        ValuatorCalculator calculator = new ValuatorCalculator(_redis);
        string similarityKey = "SIMILARITY-" + id;
        bool isSimilar = calculator.CheckSimilarity(text);
        db.StringSet(similarityKey, isSimilar.ToString());

        string textKey = "TEXT-" + id;
        await db.StringSetAsync(textKey, text);

        await _rabbitMqService.PublishSimilarityCalculatedEventAsync(id, isSimilar);
        
        await _rabbitMqService.PublishMessageAsync("valuator.processing.rank" ,id);
        
        Console.WriteLine($"text: {text}");

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