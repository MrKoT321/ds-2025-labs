using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StackExchange.Redis;
using System.Text;

class Program
{
    private const string QueueName = "valuator.processing.rank1";
    private const string HostName = "localhost:6379";

    private static ConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(HostName);

    public static async Task Main(string[] args)
    {
        ConnectionFactory factory = new ConnectionFactory
        {
            HostName = "localhost",
        };
        await using IConnection connection = await factory.CreateConnectionAsync();
        await using IChannel channel = await connection.CreateChannelAsync();

        await DeclareTopologyAsync(channel);
        string consumerTag = await RunConsumer(channel);

        Console.WriteLine("Press Enter to exit");
        Console.ReadLine();

        await channel.BasicCancelAsync(consumerTag);
    }

    private static async Task<string> RunConsumer(IChannel channel)
    {
        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += (_, eventArgs) => ConsumeAsync(channel, eventArgs);
        return await channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: false,
            consumer: consumer
        );
    }

    private static async Task ConsumeAsync(IChannel channel, BasicDeliverEventArgs eventArgs)
    {
        string id = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        Console.WriteLine($"Consuming: {id} from subject {eventArgs.Exchange}");

        string text = GetTextFromRedis(id);
        double rank = CalculateRank(text);
        SetRankInRedis(id, rank);

        await channel.BasicAckAsync(eventArgs.DeliveryTag, false);
    }

    private static string GetTextFromRedis(string id)
    {
        IDatabase redisDatabase = _redis.GetDatabase();
        string textKey = "TEXT-" + id;
        RedisValue result = redisDatabase.StringGet(textKey);
        return result.ToString();
    }

    private static void SetRankInRedis(string id, double rank)
    {
        IDatabase redisDatabase = _redis.GetDatabase();
        string rankKey = "RANK-" + id;
        redisDatabase.StringSet(rankKey, rank.ToString());
    }

    /// <summary>
    ///  Определяет топологию: queue -> consumer.
    /// </summary>
    private static async Task DeclareTopologyAsync(IChannel channel)
    {
        await channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false
        );
    }

    private static double CalculateRank(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0.0;
        }

        int nonAlphabeticSymbols = 0;

        foreach (var x in text.EnumerateRunes())
        {
            if (x.Utf16SequenceLength > 1)
            {
                ++nonAlphabeticSymbols;
                continue;
            }

            if (!IsAlphabetic((char)x.Value))
            {
                ++nonAlphabeticSymbols;
            }
        }

        return (double)nonAlphabeticSymbols / text.EnumerateRunes().Count();
    }

    private static bool IsAlphabetic(char c)
    {
        return c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= 'а' and <= 'я' or >= 'А' and <= 'Я' or 'ё' or 'Ё';
    }
}