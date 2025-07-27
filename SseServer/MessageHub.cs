using System.Collections.Concurrent;

namespace SseServer;

public class MessageHub
{
    private readonly ConcurrentDictionary<Guid, StreamWriter> _clients = new();

    public Guid RegisterClient(StreamWriter sw)
    {
        var id = Guid.NewGuid();
        _clients.TryAdd(id, sw);
        return id;
    }

    public void UnregisterClient(Guid id)
    {
        _clients.TryRemove(id, out _);
    }

    public async Task BroadcastAsync(string msg)
    {
        var toRemove = new List<Guid>();

        foreach (var (id, writer) in _clients)
        {
            try
            {
                await writer.WriteAsync($"data: {msg}\n\n");
                await writer.FlushAsync();
            }
            catch
            {
                toRemove.Add(id); 
            }
        }

        foreach (var id in toRemove)
        {
            UnregisterClient(id);
        }
    }
}
