using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using StackExchange.Redis;

namespace Valuator.Calculator;

public class ValuatorCalculator
{
    private readonly IConnectionMultiplexer _redis;

    public ValuatorCalculator(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }
    
    
    public static double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0.0;
        }

        int nonAlphabeticSymbols = 0;
        
        foreach (var x in text.EnumerateRunes())
        {
            if (x.Utf16SequenceLength > 1)
            {
                ++nonAlphabeticSymbols;
                continue;
            }
            if (!IsAlphabetic((char)x.Value))
            {
                ++nonAlphabeticSymbols;
            }
        }
        
        return (double)nonAlphabeticSymbols / text.EnumerateRunes().Count();;
    }

    public bool CheckSimilarity(string newText)
    {
        IDatabase db = _redis.GetDatabase();

        string textHash = TextToHash(newText);
        bool isDuplicate = db.SetContains("HASH-TEXTS", textHash);
        if (isDuplicate)
        {
            return true;
        }

        db.SetAdd("HASH-TEXTS", textHash);
        db.StringSet($"TEXT-BY-HASH-{textHash}", newText);
        return false;
    }
    
    private static bool IsAlphabetic(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= 'а' and <= 'я' or >= 'А' and <= 'Я' or 'ё' or 'Ё';
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