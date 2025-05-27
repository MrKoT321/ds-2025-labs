using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using RabbitMQ.Client.Events;
using RankCalculator.Hubs;

namespace RankCalculator.Services;

public class RankProcessor(IMessageChannel channel, IRedisService redisService, IHubContext<RankHub> hubContext)
{
    public async Task HandleMessageAsync(BasicDeliverEventArgs eventArgs)
    {
        await CustomDelay();

        string id = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        string text = await redisService.GetTextAsync(id);
        double rank = Services.RankCalculatorService.CalculateRank(text);
        await redisService.SetRankAsync(id, rank);

        var eventData = new { EventType = "RankCalculated", TextId = id, Rank = rank };
        byte[] eventBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventData));

        await channel.PublishAsync("events.logger", "RankCalculated", eventBody);
        await hubContext.Clients.All.SendAsync("RankCalculated", new { TextId = id, Rank = rank });

        await channel.AckAsync(eventArgs.DeliveryTag);
    }

    private static async Task CustomDelay()
    {
        TimeSpan interval = TimeSpan.FromSeconds(new Random().Next(3, 15));
        Console.WriteLine($"Waiting {interval}");
        await Task.Delay(interval);
    }
}