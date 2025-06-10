using RankCalculator.Services;

namespace Valuator.Tests.Unit.Calculator;

public class RankCalculatorTest
{
    [Theory]
    [MemberData(nameof(CalculateRankData))]
    public void CalculateRank(string text, double expected)
    {
        Assert.Equal(expected, RankCalculatorService.CalculateRank(text), 4);
    }

    public static TheoryData<string, double> CalculateRankData() =>
        new()
        {
            { "", 0 },
            { "abcdefghIjklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZабвгдеёжзийклмнопрстуфхцчшщъыьэюяАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ", 0 },
            { "[]|:)(*&/\n ", 1 },
            { "good text why not", 0.1765 },
            { "\ud83d\ude01\ud83d\ude01\ud83d\ude01", 1 },
            { "\ud83d\udcaaQ", 0.5 },
            { "\ud83d\udcaa!!", 1 },
            { "hip hip hooray \ud83d\ude2d\ud83d\ude2d\ud83d\ude2d", 0.3333 }
        };
}