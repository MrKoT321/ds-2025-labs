using Microsoft.AspNetCore.SignalR;

namespace RankCalculator.Hubs;

public class RankHub : Hub
{
    public async Task Send(string textId, double rank)
    {
        await Clients.All.SendAsync("RankCalculated", new { TextId = textId, Rank = rank });
    }
}