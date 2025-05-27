using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace Valuator.Calculators;

public class ValuatorCalculator
{
    private readonly IConnectionMultiplexer _redis;

    public ValuatorCalculator(IConnectionMultiplexer redis)
    {
        _redis = redis;
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