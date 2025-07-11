namespace ReQueue.Interfaces;

public interface IProducer
{
    public Task<string> PublishAsync(Dictionary<string, string> message);
    public Task DeleteAsync();
}