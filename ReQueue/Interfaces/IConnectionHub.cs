namespace ReQueue.Interfaces;

public interface IConnectionHub
{
    public IProducer GetMessageProducer(string redisKey, bool autoDelete = false);
    public IConsumer GetMessageConsumer(string redisKey, string consumerGroup, string consumerName,  TimeSpan pollInterval, int channelCapacity = 10000);
}