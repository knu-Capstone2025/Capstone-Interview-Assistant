using InterviewAssistant.ApiService.Storage;

namespace InterviewAssistant.ApiService.Storage;

public class MemoryStorage : IMemoryStorage
{
    private readonly Dictionary<string, string> _storage;

    public MemoryStorage()
    {
        _storage = new Dictionary<string, string>();
    }

    public void StoreContent(string key, string content)
    {
        _storage[key] = content;
    }

    public string? GetContent(string key)
    {
        if (_storage.TryGetValue(key, out string? content))
        {
            return content;
        }
        return null;
    }

    public bool ContainsKey(string key)
    {
        return _storage.ContainsKey(key);
    }
}
