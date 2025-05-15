using RabbitMQ.Client;
using System.Text;

namespace Valuator.Services
{
    public class RabbitMqService : IRabbitMqService
    {
        private string QueueName { get; set; }
        private string HostName { get; set; }

        public RabbitMqService(string queueName, string hostName)
        {
            QueueName = queueName;
            HostName = hostName;
        }

        public async Task SendMessage(string message)
        {
            CancellationTokenSource cts = new();
            Task produceTask = ProduceAsync(cts.Token, message);

            await produceTask;
            cts.Cancel();
        }

        private async Task ProduceAsync(CancellationToken ct, string message)
        {
            // Установка соединения с RabbitMQ по адресу localhost:5672
            ConnectionFactory factory = new()
            {
                HostName = HostName
            };
            await using IConnection connection = await factory.CreateConnectionAsync(ct);
            await using IChannel channel = await connection.CreateChannelAsync(null, ct);

            await DeclareTopologyAsync(channel, ct);

            // Отправка сообщения ежесекундно в цикле.
            byte[] body = Encoding.UTF8.GetBytes(message);

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: QueueName,
                mandatory: false,
                body: body
            );

            await connection.CloseAsync(ct);
        }

        private async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
        {
            await channel.ExchangeDeclareAsync(
                exchange: QueueName,
                type: ExchangeType.Direct,
                cancellationToken: ct
            );
            await channel.QueueDeclareAsync(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: ct
            );
            await channel.QueueBindAsync(
                queue: QueueName,
                exchange: QueueName,
                routingKey: "",
                cancellationToken: ct);
        }
    }
}