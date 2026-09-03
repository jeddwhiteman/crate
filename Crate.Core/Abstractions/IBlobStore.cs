namespace Crate.Core.Abstractions;

public interface IBlobStore
{
    Task<string?> ReadAsync(string key);
    Task WriteAsync(string key, string content);
}