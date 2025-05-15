using StackExchange.Redis;
using Valuator.Services;

namespace Valuator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorPages();

        var redisConfig = builder.Configuration.GetSection("Redis");
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConfig["ConnectionString"]!));
        
        var rabbitSection = builder.Configuration.GetSection("RabbitMQ");
        var hostName = rabbitSection.GetValue<string>("HostName");
        var queueName = rabbitSection.GetValue<string>("QueueName");

        builder.Services.AddSingleton<IRabbitMqService>(provider => new RabbitMqService(queueName!, hostName!));

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

        app.Run();
    }
}