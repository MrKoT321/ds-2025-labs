using Microsoft.AspNetCore.SignalR;

namespace RankCalculator.Hub;

public class RankHub : Microsoft.AspNetCore.SignalR.Hub
{
    public async Task Send(string textId)
    {
        await this.Clients.All.SendAsync("RankCalculated", textId);
    }
}