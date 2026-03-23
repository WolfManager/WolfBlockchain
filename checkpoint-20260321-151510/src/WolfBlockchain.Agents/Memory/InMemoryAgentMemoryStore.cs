namespace WolfBlockchain.Agents.Memory;

public sealed class InMemoryAgentMemoryStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, string> _memory = new(StringComparer.Ordinal);

    public void Save(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (_sync)
        {
            _memory[key] = value;
        }
    }

    public string? Get(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        lock (_sync)
        {
            _memory.TryGetValue(key, out var value);
            return value;
        }
    }
}
