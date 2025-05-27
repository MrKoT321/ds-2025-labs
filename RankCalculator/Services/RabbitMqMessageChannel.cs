using RabbitMQ.Client.Events;

namespace RankCalculator.Services;

using RabbitMQ.Client;

public class RabbitMqMessageChannel(IChannel channel, string queueName) : IMessageChannel
{
    public async Task PublishAsync(string exchange, string routingKey, byte[] body)
    {
        await channel.BasicPublishAsync(exchange, routingKey, body);
    }

    public async Task AckAsync(ulong deliveryTag)
    {
        await channel.BasicAckAsync(deliveryTag, multiple: false);
    }

    public Task ConsumeAsync(Func<BasicDeliverEventArgs, Task> onMessage)
    {
        AsyncEventingBasicConsumer consumer = new(channel);
        consumer.ReceivedAsync += async (_, eventArgs) => await onMessage(eventArgs);

        return channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer
        );
    }
}