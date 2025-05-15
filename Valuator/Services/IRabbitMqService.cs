namespace Valuator.Services
{
    public interface IRabbitMqService
    {
        Task SendMessage(string message);
    }
}