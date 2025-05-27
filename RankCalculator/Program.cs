using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RankCalculator.Hubs;
using RankCalculator.Services;

namespace RankCalculator;

public class Program
{
    enum EventType
    {
        RankCalculated,
    }

    private const string QueueName = "valuator.processing.rank";
    private const string ExchangeName = "events.logger";
    private const string RedisConnectionString = "redis:6379";

    private static ConnectionMultiplexer? _redis;

    public static async Task Main(string[] args)
    {
        var builder = GetAppBuilder(args);

        _redis = await ConnectionMultiplexer.ConnectAsync(RedisConnectionString);

        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
        };
        await using IConnection connection = await factory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        var app = builder.Build();
        var hubContext = app.Services.GetRequiredService<IHubContext<RankHub>>();

        app.MapHub<RankHub>("/rankCalculated");
        app.UseCors();

        await DeclareTopologyAsync(channel);
        await RunConsumer(channel, hubContext);

        await app.RunAsync("http://0.0.0.0:5003");
        await Task.Delay(Timeout.Infinite);
    }
    
    

    private static WebApplicationBuilder GetAppBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy.WithOrigins("http://localhost:8080").AllowAnyHeader().AllowAnyMethod().AllowCredentials()
            )
        );
        builder.Services.AddSignalR();

        return builder;
    }

    private static async Task RunConsumer(IChannel channel, IHubContext<RankHub> hubContext)
    {
        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, eventArgs) => await ConsumeAsync(channel, hubContext, eventArgs);

        await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer
        );

        Console.WriteLine("Consumer is now consuming from queue...");
    }

    private static async Task ConsumeAsync(IChannel channel, IHubContext<RankHub> hubContext,
        BasicDeliverEventArgs eventArgs)
    {
        await CustomDelay();

        Console.WriteLine($"Start processing: {Encoding.UTF8.GetString(eventArgs.Body.ToArray())}");
        string id = Encoding.UTF8.GetString(eventArgs.Body.ToArray());

        string text = await GetTextFromRedis(id);
        double rank = RankCalculator.Services.RankCalculator.CalculateRank(text);
        await SetRankInRedis(id, rank);

        await PublishRankCalculatedEventAsync(channel, hubContext, id, rank);

        await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
    }

    private static async Task<string> GetTextFromRedis(string id)
    {
        RedisValue result = await _redis.GetDatabase().StringGetAsync("TEXT-" + id);
        return result.ToString();
    }

    private static async Task SetRankInRedis(string id, double rank)
    {
        await _redis.GetDatabase().StringSetAsync("RANK-" + id, rank.ToString());
    }

    private static async Task PublishRankCalculatedEventAsync(
        IChannel channel,
        IHubContext<RankHub> hubContext,
        string textId,
        double rank
    )
    {
        var eventData = new { EventType = EventType.RankCalculated.ToString(), TextId = textId, Rank = rank };
        var eventJson = JsonSerializer.Serialize(eventData);
        var eventBody = Encoding.UTF8.GetBytes(eventJson);

        await channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: EventType.RankCalculated.ToString(),
            body: eventBody
        );

        await hubContext.Clients.All.SendAsync("RankCalculated", new { TextId = textId, Rank = rank });

        Console.WriteLine("Event published");
    }

    private static async Task DeclareTopologyAsync(IChannel channel)
    {
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType.Fanout,
            durable: true
        );
    }

    private static async Task CustomDelay()
    {
        TimeSpan interval = TimeSpan.FromSeconds(new Random().Next(3, 15));
        Console.WriteLine($"Waiting {interval}");
        await Task.Delay(interval);
    }
}