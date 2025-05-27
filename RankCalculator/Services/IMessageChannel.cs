using RabbitMQ.Client.Events;

namespace RankCalculator.Services;

public interface IMessageChannel
{
    Task PublishAsync(string exchange, string routingKey, byte[] body);
    Task AckAsync(ulong deliveryTag);
    Task ConsumeAsync(Func<BasicDeliverEventArgs, Task> onMessage);
}