using System.Text.Json;
using Crate.Core.Abstractions;

namespace Crate.Core.Persistence;

public class JsonSeenReleaseRepository(IBlobStore blobStore, string key = "seen.json", int maxKeys = 500) : ISeenReleaseRepository
{
    public async Task<IReadOnlySet<string>> GetAllKeysAsync()
    {
        var json = await blobStore.ReadAsync(key);
        if (string.IsNullOrWhiteSpace(json)) return new HashSet<string>();

        var dto = JsonSerializer.Deserialize<SeenReleasesDto>(json);
        return new HashSet<string>(dto?.Keys ?? []);
    }

    public async Task MarkSeenAsync(IEnumerable<string> keys)
    {
        var existing = await GetAllKeysAsync();
        var all = new List<string>(existing);
        
        foreach (var k in keys)
            if (!existing.Contains(k))
                all.Add(k);
        
        var trimmed = all.Count > maxKeys ? all.Skip(all.Count - maxKeys) : all;
        await blobStore.WriteAsync(key, JsonSerializer.Serialize(new SeenReleasesDto(trimmed)));
    }
}