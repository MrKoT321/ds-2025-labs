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
    private const string QueueName = "valuator.processing.rank";
    private const string ExchangeName = "events.logger";
    private const string RedisConnectionString = "redis:6379";

    public static async Task Main(string[] args)
    {
        var builder = GetAppBuilder(args);

        builder.Services.AddSingleton<IRedisService>(_ =>
        {
            var multiplexer = ConnectionMultiplexer.Connect(RedisConnectionString);
            return new RedisService(multiplexer);
        });

        builder.Services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory { HostName = "rabbitmq" });

        var app = builder.Build();
        var rabbitFactory = app.Services.GetRequiredService<IConnectionFactory>();
        await using IConnection connection = await rabbitFactory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        var redisService = app.Services.GetRequiredService<IRedisService>();
        var hubContext = app.Services.GetRequiredService<IHubContext<RankHub>>();

        app.MapHub<RankHub>("/rankCalculated");
        app.UseCors();

        await DeclareTopologyAsync(channel);

        var messageChannel = new RabbitMqMessageChannel(channel, QueueName);
        var processor = new RankProcessor(messageChannel, redisService, hubContext);

        await messageChannel.ConsumeAsync(processor.HandleMessageAsync);

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
}