namespace RankCalculator.Services;

public static class RankCalculator
{
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

        return (double)nonAlphabeticSymbols / text.EnumerateRunes().Count();
    }
    
    
    private static bool IsAlphabetic(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= 'а' and <= 'я' or >= 'А' and <= 'Я' or 'ё' or 'Ё';
    }
}