using RabbitMQ.Client;
using RankCalculator.Hub;
using StackExchange.Redis;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddSignalR();

        var redisConfig = builder.Configuration.GetSection("Redis");
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConfig["ConnectionString"]!));
        
        var rabbitSection = builder.Configuration.GetSection("RabbitMQ");
        var hostName = rabbitSection.GetValue<string>("HostName");
        var factory = new ConnectionFactory { HostName = hostName! };
        var rabbitMqConnection = await factory.CreateConnectionAsync();
        builder.Services.AddSingleton(rabbitMqConnection);
        
        builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapRazorPages();
        
        app.MapHub<RankHub>("/chat");

        app.Run();
    }
}