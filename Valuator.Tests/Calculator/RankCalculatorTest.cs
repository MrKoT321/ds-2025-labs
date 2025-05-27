using RankCalculator;

namespace Valuator.Tests.Calculator;

public class RankCalculatorTest
{
    [Theory]
    [MemberData(nameof(CalculateRankData))]
    public void CalculateRank(string text, double expected)
    {
        Assert.Equal(expected, RankCalculator.Services.RankCalculator.CalculateRank(text), 4);
    }
    
    public static IEnumerable<object[]> CalculateRankData()
    {
        yield return ["", 0];
        yield return ["abcdefghIjklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZабвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ", 0];
        yield return ["[]|:)(*& ", 1];
        yield return ["good text why not", 0.1765];
        yield return ["\ud83d\ude01\ud83d\ude01\ud83d\ude01", 1];
        yield return ["\ud83d\udcaaQ", 0.5];
        yield return ["\ud83d\udcaa!!", 1];
        yield return ["hip hip hooray \ud83d\ude2d\ud83d\ude2d\ud83d\ude2d", 0.3333];
    }
}
