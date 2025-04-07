using Valuator.Calculator;

namespace Valuator.Tests.Calculator;

public class ValuatorCalculatorTest
{
    [Theory]
    [MemberData(nameof(CalculateRankData))]
    public void CalculateRank(string text, double expected)
    {
        Assert.Equal(expected, ValuatorCalculator.CalculateRank(text), 4);
    }
    
    public static IEnumerable<object[]> CalculateRankData()
    {
        yield return ["good text", 0.1111];
        yield return ["", 0];
        yield return ["[]|:)(*&", 1];
        yield return ["TheQuickBrownFoxJumpsOverTheLazyDog", 0];
        yield return ["\ud83d\ude01\ud83d\ude01\ud83d\ude01", 1];
        yield return ["\ud83d\udcaaQ", 0.5];
        yield return ["hip hip hooray \ud83d\ude2d\ud83d\ude2d\ud83d\ude2d", 0.3333];
    }
}
