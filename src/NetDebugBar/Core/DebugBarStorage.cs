using System.Collections.Concurrent;

namespace NetDebugBar.Core;

public class DebugBarStorage
{
    private readonly ConcurrentDictionary<string, (NetDebugBarContextSnapshot Snapshot, DateTimeOffset Created)> _store = new();

    public string Store(NetDebugBarContextSnapshot snapshot)
    {
        var id = Guid.NewGuid().ToString("N");
        _store[id] = (snapshot, DateTimeOffset.UtcNow);
        Cleanup();
        return id;
    }

    public NetDebugBarContextSnapshot? Retrieve(string id)
    {
        if (_store.TryRemove(id, out var entry))
            return entry.Snapshot;
        return null;
    }

    private void Cleanup()
    {
        var cutoff = DateTimeOffset.UtcNow.AddSeconds(-60);
        foreach (var key in _store.Keys)
        {
            if (_store.TryGetValue(key, out var entry) && entry.Created < cutoff)
                _store.TryRemove(key, out _);
        }
    }
}