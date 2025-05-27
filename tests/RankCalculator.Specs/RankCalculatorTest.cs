using System.Text;
using Microsoft.AspNetCore.SignalR;
using Moq;
using RabbitMQ.Client.Events;
using TechTalk.SpecFlow;
using RankCalculator.Hubs;
using System.Runtime.Serialization;
using System.Reflection;
using RankCalculator.Services;
using StackExchange.Redis;

namespace RankCalculator.Specs;

[Binding]
public class RankProcessingSteps : IDisposable
{
    private readonly IDatabase _redisDb;
    private readonly ConnectionMultiplexer _redis;
    
    private readonly Mock<IMessageChannel> _channelMock = new();
    private readonly Mock<IHubContext<RankHub>> _hubMock = new();
    private readonly Mock<IHubClients> _clientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();

    private RankProcessor _processor;
    private BasicDeliverEventArgs _eventArgs;
    private double _calculatedRank = -1;
    
    private readonly string RedisTestConfiguration = "localhost:6380";

    public RankProcessingSteps()
    {
        _redis = ConnectionMultiplexer.Connect(RedisTestConfiguration);
        _redisDb = _redis.GetDatabase();
    }

    public void Dispose()
    {
        _redis.Dispose();
    }

    [Given(@"a text with ID ""(.*)"" exists in Redis with content ""(.*)""")]
    public async Task GivenATextWithIDExistsInRedisWithContent(string id, string text)
    {
        await _redisDb.StringSetAsync("TEXT-" + id, text);

        _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientProxyMock.Setup(c => c.SendCoreAsync("RankCalculated", It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        _channelMock.Setup(c => c.PublishAsync("events.logger", "RankCalculated", It.IsAny<byte[]>()))
            .Returns(Task.CompletedTask);
        _channelMock.Setup(c => c.AckAsync(It.IsAny<ulong>())).Returns(Task.CompletedTask);

        var redisService = new RedisService(_redis);
        _processor = new RankProcessor(_channelMock.Object, redisService, _hubMock.Object);
    }

    [When(@"a message with body ""(.*)"" is received")]
    public async Task WhenAMessageWithBodyIsReceived(string messageId)
    {
        var bytes = Encoding.UTF8.GetBytes(messageId);
        _eventArgs = (BasicDeliverEventArgs)FormatterServices.GetUninitializedObject(typeof(BasicDeliverEventArgs));

        typeof(BasicDeliverEventArgs).GetField("Body", BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(_eventArgs, new ReadOnlyMemory<byte>(bytes));

        typeof(BasicDeliverEventArgs).GetField("DeliveryTag", BindingFlags.Instance | BindingFlags.Public)
            ?.SetValue(_eventArgs, 1UL);

        await _processor.HandleMessageAsync(_eventArgs);
    }

    [Then(@"the rank should be calculated and stored in Redis with ID ""(.*)"" and Value ""(.*)""")]
    public async Task ThenTheRankShouldBeCalculatedAndStoredInRedisWithIDAndValue(string id, double value)
    {
        _calculatedRank = await new RedisService(_redis).GetRankAsync(id);
        Assert.Equal(_calculatedRank, value);
    }

    [Then(@"an event should be published to RabbitMQ with routing key ""(.*)""")]
    public void ThenAnEventShouldBePublishedToRabbitMQWithRoutingKey(string routingKey)
    {
        _channelMock.Verify(c => c.PublishAsync("events.logger", routingKey, It.IsAny<byte[]>()), Times.Once);
    }

    [Then(@"the SignalR hub should broadcast ""(.*)"" with the correct rank")]
    public void ThenTheSignalRHubShouldBroadcastWithTheCorrectRank(string method)
    {
        _clientProxyMock.Verify(c =>
                c.SendCoreAsync(method, It.IsAny<object[]>(), default),
            Times.Once
        );
    }
}