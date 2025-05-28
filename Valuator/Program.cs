using RabbitMQ.Client;
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

        var redisConfig = builder.Configuration.GetSection("Redis");
        builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConfig["ConnectionString"]!));

        var rabbitSection = builder.Configuration.GetSection("RabbitMQ");
        var hostName = rabbitSection.GetValue<string>("HostName");

        builder.Services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory { HostName = hostName! });

        // builder.Services.AddSingleton<IConnection>(async (sp) =>
        // {
            // var factory = sp.GetRequiredService<IConnectionFactory>();
            // return await factory.CreateConnectionAsync();
        // });
        
        RabbitMqService rabbitMqService = new(hostName!);
        builder.Services.AddSingleton<IRabbitMqService>(rabbitMqService);

        // var factory = new ConnectionFactory { HostName = hostName! };
        // var rabbitMqConnection = await factory.CreateConnectionAsync();
        // builder.Services.AddSingleton(rabbitMqConnection);

        // builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

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
        
        builder.WebHost.UseUrls("http://0.0.0.0:8080");

        app.Run();
    }
}