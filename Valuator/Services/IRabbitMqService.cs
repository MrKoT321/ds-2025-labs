namespace Valuator.Services;

public interface IRabbitMqService
{
    Task PublishMessageAsync(string queueName, string message);
    Task PublishSimilarityCalculatedEventAsync(string textId, bool isSimilar);
}