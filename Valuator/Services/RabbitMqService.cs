using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Valuator.Services;

public class RabbitMqService(IConnection rabbitMqConnection) : IRabbitMqService
{
    private const string ExchangeName = "events.logger";
    
    public async Task PublishMessageAsync(string queueName, string message)
    {
        await using var channel = await rabbitMqConnection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queueName, true, false, false);

        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync("", queueName, body);

        await Task.CompletedTask;
    }

    public async Task PublishSimilarityCalculatedEventAsync(string textId, bool isSimilar)
    {
        await using var channel = await rabbitMqConnection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Fanout, true);

        var eventData = new { EventType = EventType.SimilarityCalculated.ToString(), TextId = textId, Similarity = isSimilar};
        var eventJson = JsonSerializer.Serialize(eventData);
        var body = Encoding.UTF8.GetBytes(eventJson);

        await channel.BasicPublishAsync(ExchangeName, EventType.SimilarityCalculated.ToString(), body);
    }
}