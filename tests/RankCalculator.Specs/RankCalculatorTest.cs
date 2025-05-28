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
    private readonly ConnectionMultiplexer _redis;
    
    private readonly Mock<IMessageChannel> _channelMock = new();
    private readonly Mock<IHubContext<RankHub>> _hubMock = new();
    private readonly Mock<IHubClients> _clientsMock = new();
    private readonly Mock<IClientProxy> _clientProxyMock = new();

    private RedisService _redisService;
    private RankProcessor _processor;
    private BasicDeliverEventArgs _eventArgs;

    private const string RedisTestConfiguration = "localhost:6380";

    public RankProcessingSteps()
    {
        _redis = ConnectionMultiplexer.Connect(RedisTestConfiguration);
    }

    public void Dispose()
    {
        _redis.Dispose();
    }

    [Given(@"a text with ID ""(.*)"" exists with content ""(.*)""")]
    public async Task GivenATextWithIDExistsWithContent(string id, string text)
    {
        await _redis.GetDatabase().StringSetAsync("TEXT-" + id, text);

        _clientsMock.Setup(c => c.All).Returns(_clientProxyMock.Object);
        _hubMock.Setup(h => h.Clients).Returns(_clientsMock.Object);
        _clientProxyMock.Setup(c => c.SendCoreAsync("RankCalculated", It.IsAny<object[]>(), default))
            .Returns(Task.CompletedTask);

        _channelMock.Setup(c => c.PublishAsync("events.logger", "RankCalculated", It.IsAny<byte[]>()))
            .Returns(Task.CompletedTask);
        _channelMock.Setup(c => c.AckAsync(It.IsAny<ulong>())).Returns(Task.CompletedTask);

        _redisService = new RedisService(_redis);
        _processor = new RankProcessor(_channelMock.Object, _redisService, _hubMock.Object);
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

    [Then(@"the rank should be calculated and stored with ID ""(.*)"" and Value ""(.*)""")]
    public async Task ThenTheRankShouldBeCalculatedAndStoredWithIDAndValue(string id, double value)
    {
        Assert.Equal(await _redisService.GetRankAsync(id), value);
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