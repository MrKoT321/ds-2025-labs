using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;
using Valuator.Services;

namespace Valuator.Calculators;

public class ValuatorCalculator
{
    private readonly IStorageService _storageService;

    public ValuatorCalculator(IStorageService storageService)
    {
        _storageService = storageService;
    }

    public bool CheckSimilarity(string newText, string id)
    {
        string textHash = TextToHash(newText);
        Console.WriteLine(textHash);
        bool isDuplicate = _storageService.SaveContainsById("HASH-TEXTS", id, textHash);
        if (isDuplicate)
        {
            return true;
        }

        _storageService.SaveById($"TEXT-BY-HASH-{textHash}", id, textHash);
        return false;
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