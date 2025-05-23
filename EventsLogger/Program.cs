using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

public class EventsLoggerConsuemr
{
    private enum EventType
    {
        RankCalculated,
        SimilarityCalculated
    }
    
    private const string QueueEventsBase = "valuator.events";
    private const string ExchangeName = "events.logger";

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Consumer started");

        var factory = new ConnectionFactory { HostName = "rabbitmq" };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        string queueName = GetQueueName();
        await DeclareTopologyAsync(channel, queueName);
        await RunConsumer(channel, queueName);

        Console.WriteLine("EventsLogger is running.");
        await Task.Delay(Timeout.Infinite);
    }

    private static async Task RunConsumer(IChannel channel, string queueName)
    {
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, eventArgs) =>
        {
            var message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            try
            {
                JsonSerializerOptions options = new() { WriteIndented = true };
                var json = JsonSerializer.Deserialize<JsonElement>(message);
                Console.WriteLine($"{JsonSerializer.Serialize(json, options)}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to process message: {e.Message}");
            }
            
            await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
            await Task.CompletedTask;
        };
        
        await channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer
        );
    }
    
    private static async Task DeclareTopologyAsync(IChannel channel, string queueName)
    {
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false
        );

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: false,
            exclusive: false,
            autoDelete: true
        );
        
        await channel.QueueBindAsync(
            queue: queueName,
            routingKey: "",
            exchange: ExchangeName
        );
    }

    private static string GetQueueName()
    {
        string? instanceId = Environment.GetEnvironmentVariable("INSTANCE_ID");
        if (string.IsNullOrEmpty(instanceId))
        {
            throw new ArgumentException("Please set environment variable INSTANCE_ID");
        }
        return QueueEventsBase + "." + instanceId;
    }
}