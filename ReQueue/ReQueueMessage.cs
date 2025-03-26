using StackExchange.Redis;

namespace ReQueue;

public class ReQueueMessage
{
    public string Id { get; set; }
    public Dictionary<string, string> Values { get; set; }

    public static ReQueueMessage FromStreamEntry(StreamEntry entry)
    {
        var values = new Dictionary<string, string>();
        foreach (var val in entry.Values)
        {
            values[val.Name] = val.Value;
        }

        return new ReQueueMessage()
        {
            Id = entry.Id,
            Values = values
        };
    }
}