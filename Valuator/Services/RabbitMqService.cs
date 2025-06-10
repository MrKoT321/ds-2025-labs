using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Valuator.Services;

public class RabbitMqService(IConfigurationRoot configuration) : IRabbitMqService
{
    private IConnection? _rabbitMqConnection;

    private const string ExchangeName = "events.logger";

    public async Task PublishMessageAsync(string queueName, string message)
    {
        IConnection rabbitMqConnection = await GetConnection();
        await using var channel = await rabbitMqConnection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queueName, true, false, false);

        var body = Encoding.UTF8.GetBytes(message);
        await channel.BasicPublishAsync("", queueName, body);

        await Task.CompletedTask;
    }

    public async Task PublishSimilarityCalculatedEventAsync(string textId, bool isSimilar)
    {
        IConnection rabbitMqConnection = await GetConnection();
        await using var channel = await rabbitMqConnection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Fanout, true);

        var eventData = new
        {
            EventType = EventType.SimilarityCalculated.ToString(), TextId = textId, Similarity = isSimilar
        };
        var eventJson = JsonSerializer.Serialize(eventData);
        var body = Encoding.UTF8.GetBytes(eventJson);

        await channel.BasicPublishAsync(ExchangeName, EventType.SimilarityCalculated.ToString(), body);
    }

    private async Task<IConnection> GetConnection()
    {
        if (_rabbitMqConnection != null) return _rabbitMqConnection;
        
        var factory = new ConnectionFactory
        {
            HostName = configuration["RabbitMQ:Hostname"]!,
            UserName = configuration["RabbitMQ:UserName"]!,
            Password = configuration["RabbitMQ:Password"]!
        };
        _rabbitMqConnection = await factory.CreateConnectionAsync();

        return _rabbitMqConnection;
    }
}