using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using System.Text;
using Valuator.Calculators;
using Valuator.Services;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly IRabbitMqService _rabbitMqService;

    [BindProperty(Name = "text")]
    public string Text { get; set; } = string.Empty;
    
    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis, IRabbitMqService rabbitMqService)
    {
        _logger = logger;
        _redis = redis;
        _rabbitMqService = rabbitMqService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Text))
        {
            _logger.LogError("Text is null or empty in POST.");
            return BadRequest();
        }
        _logger.LogDebug(Text);

        string id = TextToHash(Text);
        IDatabase db = _redis.GetDatabase();

        ValuatorCalculator calculator = new ValuatorCalculator(_redis);
        string similarityKey = "SIMILARITY-" + id;
        bool isSimilar = calculator.CheckSimilarity(Text);
        db.StringSet(similarityKey, isSimilar.ToString());

        string textKey = "TEXT-" + id;
        await db.StringSetAsync(textKey, Text);

        await _rabbitMqService.PublishSimilarityCalculatedEventAsync(id, isSimilar);

        await _rabbitMqService.PublishMessageAsync("valuator.processing.rank", id);

        Console.WriteLine($"Text: {Text}");

        return Redirect($"summary?id={id}");
    }

    private string TextToHash(string Text)
    {
        SHA256 sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(Text));

        var builder = new StringBuilder();
        foreach (byte b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }

        return builder.ToString();
    }
}