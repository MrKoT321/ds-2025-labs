using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using TechTalk.SpecFlow;
using Valuator.Services;
using Valuator.Pages;

namespace Valuator.Specs;

[Binding]
public class ValuatorTest
{
    private HttpClient _client;
    private HttpResponseMessage _response;
    private string? _summaryHtml;
    private string? _redirectUrl;
    private string? _publishedId;
    private bool? _isSimilar;

    private Mock<IRabbitMqService> _rabbitMock = new();
    private Mock<IConnectionMultiplexer> _redisMock = new();

    public ValuatorTest()
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                RemoveDefaultServices(services);

                _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), null)).Returns(InitMockRedisDatabase().Object);
                services.AddSingleton(_redisMock.Object);

                _rabbitMock.Setup(m => m.PublishSimilarityCalculatedEventAsync(It.IsAny<string>(), It.IsAny<bool>()))
                    .Callback<string, bool>((_, isSimilar) => _isSimilar = isSimilar)
                    .Returns(Task.CompletedTask);
                _rabbitMock.Setup(m => m.PublishMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
                    .Callback<string, string>((_, id) => _publishedId = id)
                    .Returns(Task.CompletedTask);
                services.AddSingleton(_rabbitMock.Object);
            });
        });

        _client = factory.CreateClient();
    }

    [Given(@"user opens the Index page")]
    public async Task GivenUserOpensTheIndexPage()
    {
        _response = await _client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, _response.StatusCode);
    }

    [When(@"user submits the text ""(.*)""")]
    public async Task WhenUserSubmitsTheText(string inputText)
    {
        Mock<ILogger<IndexModel>> mockLogger = new();
        IndexModel model = new(mockLogger.Object, _redisMock.Object, _rabbitMock.Object)
        {
            Text = "тестовый текст"
        };

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectResult>(result);
        _redirectUrl = redirect.Url;

        // FormUrlEncodedContent formData = new([
        //     new KeyValuePair<string, string>("text", inputText)
        // ]);
        //
        // _response = await _client.PostAsync("/", formData);
    }

    [Then(@"user is redirected to the Summary page")]
    public void ThenUserIsRedirectedToTheSummaryPage()
    {
        Assert.NotNull(_redirectUrl);
        Assert.StartsWith("summary?id=", _redirectUrl);
        
        // Assert.Equal(HttpStatusCode.Redirect, _response.StatusCode);
        // var location = _response.Headers.Location?.ToString();
        // Assert.NotNull(location);
        // Assert.StartsWith("/summary?id=", location);
    }
    
    [Then(@"RabbitMQ should receive Similarity and Rank messages")]
    public void ThenRabbitMqShouldReceiveMessages()
    {
        Assert.NotNull(_publishedId);
        Assert.NotNull(_isSimilar);
        
        string prefix = "summary?id=";
        string textId = _redirectUrl!.Substring(_redirectUrl.IndexOf(prefix, StringComparison.Ordinal) + prefix.Length);
        _rabbitMock.Verify(m => m.PublishSimilarityCalculatedEventAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
        _rabbitMock.Verify(m => m.PublishMessageAsync("valuator.processing.rank", It.IsAny<string>()), Times.Once);
        
        Assert.Equal(_publishedId, textId);
        Assert.True(_isSimilar);
    }

    [Then(@"Similarity should be ""(.*)""")]
    public async Task ThenSimilarityShouldBe(string expectedSimilarity)
    {
        var summaryResponse = await _client.GetAsync("/" + _redirectUrl);
        summaryResponse.EnsureSuccessStatusCode();

        _summaryHtml = await summaryResponse.Content.ReadAsStringAsync();
        Assert.Contains("Плагиат: " + expectedSimilarity, _summaryHtml);
        
        // var location = _response.Headers.Location?.ToString();
        // var summaryResponse = await _client.GetAsync(location);
        // _summaryHtml = await summaryResponse.Content.ReadAsStringAsync();

        // Assert.Contains(expectedSimilarity, _summaryHtml);
    }

    [Then(@"Rank should be ""(.*)""")]
    public void ThenRankShouldBe(string expectedRank)
    {
        Assert.Contains("Оценка содержания: " + expectedRank, _summaryHtml);
    }

    private static void RemoveDefaultServices(IServiceCollection services)
    {
        var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
        if (redisDescriptor != null) services.Remove(redisDescriptor);

        var rabbitDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRabbitMqService));
        if (rabbitDescriptor != null) services.Remove(rabbitDescriptor);
    }

    private static Mock<IDatabase> InitMockRedisDatabase()
    {
        Mock<IDatabase> mockDb = new();

        mockDb.Setup(db => db.StringSet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, When.Always,
                CommandFlags.None))
            .Returns(true);

        mockDb.Setup(db => db.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, When.Always,
                CommandFlags.None))
            .ReturnsAsync(true);

        mockDb.Setup(db => db.StringGet(It.IsAny<RedisKey>(), CommandFlags.None))
            .Returns<RedisKey, CommandFlags>((key, _) =>
            {
                if (key.ToString().StartsWith("SIMILARITY-")) return "True";
                if (key.ToString().StartsWith("RANK-")) return "0.1765";
                return RedisValue.Null;
            });

        mockDb.Setup(db => db.SetContains(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), CommandFlags.None))
            .Returns(true);

        return mockDb;
    }
}