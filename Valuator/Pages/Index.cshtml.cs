using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using Valuator.Calculators;
using Valuator.Services;

namespace Valuator.Pages;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IStorageService _storageService;
    private readonly IRabbitMqService _rabbitMqService;

    private static readonly string[] Countries = ["RU", "EU", "Asia"];

    [BindProperty(Name = "text")] public string Text { get; set; } = string.Empty;
    [BindProperty(Name = "country")] public string Country { get; set; } = string.Empty;

    public IndexModel(ILogger<IndexModel> logger, IStorageService storageService, IRabbitMqService rabbitMqService)
    {
        _logger = logger;
        _storageService = storageService;
        _rabbitMqService = rabbitMqService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == null)
        {
            return RedirectToPage("/Login");
        }
        
        if (string.IsNullOrWhiteSpace(Text) || string.IsNullOrWhiteSpace(Country))
        {
            _logger.LogError("Invalid POST params.");
            return BadRequest();
        }

        if (!Countries.Contains(Country))
        {
            _logger.LogError("Invalid Country passed.");
            return BadRequest();
        }

        _logger.LogDebug(Text);
        
        string id = TextToHash();
        _storageService.SaveShardKey(id, Country);
        Console.WriteLine(id);
        Console.WriteLine($"UserId: {currentUserId}");

        ValuatorCalculator calculator = new ValuatorCalculator(_storageService);
        string similarityKey = "SIMILARITY-" + id;
        bool isSimilar = calculator.CheckSimilarity(Text, Country);
        _storageService.SaveById(similarityKey, isSimilar.ToString(), id);

        string textKey = "TEXT-" + id;
        _storageService.SaveByShardKey(textKey, Text, Country);
        string userKey = "USER-" + id;
        _storageService.SaveByShardKey(userKey, currentUserId, Country);

        await _rabbitMqService.PublishSimilarityCalculatedEventAsync(id, isSimilar);

        await _rabbitMqService.PublishMessageAsync("valuator.processing.rank", id + "|" + Country);

        Console.WriteLine($"Text: {Text}");

        return Redirect($"summary?id={id}");
    }

    private string TextToHash()
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