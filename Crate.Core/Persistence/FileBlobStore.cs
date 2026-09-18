using Crate.Core.Abstractions;

namespace Crate.Core.Persistence;

public class FileBlobStore(string directory) : IBlobStore
{
    public async Task<string?> ReadBlobAsync(string key)
    {
        var path = Path.Combine(directory, key);
        return File.Exists(path) ? await File.ReadAllTextAsync(path) : null;
    }

    public async Task WriteBlobAsync(string key, string content)
    {
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, key), content);

    }
}