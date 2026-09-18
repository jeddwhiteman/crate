namespace Crate.Core.Abstractions;

public interface IBlobStore
{
    Task<string?> ReadBlobAsync(string key);
    Task WriteBlobAsync(string key, string content);
}