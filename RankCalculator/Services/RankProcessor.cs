using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client.Events;
using RankCalculator.Hubs;

namespace RankCalculator.Services;

public class RankProcessor(IMessageChannel channel, IRedisService redisService, IHubContext<RankHub> hubContext)
{
    private const string ExchangeName = "events.logger";
    private const string RoutingKey = "RankCalculated";
    private const string EventName = "RankCalculated";
        
    public async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs)
    {
        string[] parts = Encoding.UTF8.GetString(eventArgs.Body.ToArray()).Split('|');
        string id = parts[0];
        string country = parts[1];
        
        // await CustomDelay();

        string text = await redisService.GetTextAsyncByShardKey(id, country);
        double rank = RankCalculatorService.CalculateRank(text);
        await redisService.SetRankAsyncSharded(id, rank, country);

        var eventData = new { EventType = EventName, TextId = id, Rank = rank };
        byte[] eventBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventData));

        await channel.PublishAsync(ExchangeName, RoutingKey, eventBody);
        await hubContext.Clients.All.SendAsync(EventName, new { TextId = id, Rank = rank });

        await channel.AckAsync(eventArgs.DeliveryTag);
    }

    private static async Task CustomDelay()
    {
        TimeSpan interval = TimeSpan.FromSeconds(new Random().Next(3, 15));
        Console.WriteLine($"Waiting {interval}");
        await Task.Delay(interval);
    }
}