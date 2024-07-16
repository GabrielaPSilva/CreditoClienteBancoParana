namespace Produtor.Services.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync(string queue, object message);
    }
}
