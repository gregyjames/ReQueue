namespace ReQueue.Interfaces;

public interface IConsumer
{
    public Task StartConsuming(CancellationToken token = default);
    public void StopConsuming();
    public event MessageReceivedHandler? OnMessageReceived;
    public Task WatchForDeletionAsync(string key, Action onDeleted, TimeSpan pollInterval = default);
}