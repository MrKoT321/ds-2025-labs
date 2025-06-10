using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Valuator.Services;

namespace Valuator.Pages;

[Authorize]
public class SummaryModel : PageModel
{
    public const string NotCompleteAssessment = "в процессе...";
    private readonly ILogger<SummaryModel> _logger;
    private readonly IStorageService _storageService;

    public SummaryModel(ILogger<SummaryModel> logger, IStorageService storageService)
    {
        _logger = logger;
        _storageService = storageService;
        Rank = NotCompleteAssessment;
        Similarity = NotCompleteAssessment;
        Id = String.Empty;
    }

    public string Rank { get; set; }
    public string Similarity { get; set; }
    public string Id { get; private set; }

    public IActionResult OnGet(string id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var textOwnerId = _storageService.GetUserIdByTextId(id);
        Console.WriteLine($"textOwnerId: {textOwnerId}");
        Console.WriteLine($"UserId: {currentUserId}");

        if (textOwnerId == null || textOwnerId != currentUserId)
        {
            return RedirectToPage("/Index");
        }
        
        Id = id;

        _logger.LogDebug(id);

        string similarityKey = "SIMILARITY-" + id;
        string? similarityStr = _storageService.GetById(Id, similarityKey);
        
        string rankKey = "RANK-" + id;
        string? rankValue = _storageService.GetById(Id, rankKey);

        if (!string.IsNullOrEmpty(similarityStr))
        {
            Similarity = similarityStr.Equals("True", StringComparison.OrdinalIgnoreCase) ? "1" : "0";
        }

        if (!string.IsNullOrEmpty(rankValue))
        {
            Rank = rankValue;
        }

        return Page();
    }
}