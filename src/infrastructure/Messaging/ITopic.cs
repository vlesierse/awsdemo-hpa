namespace Infrastructure.Messaging;

public interface ITopic
{
    Task Publish<T>(T message);
}