namespace Crate.Core.Abstractions;

public interface ISeenReleaseRepository
{
    Task<IReadOnlySet<string>> GetAllKeysAsync();
    Task MarkSeenAsync(IEnumerable<string> keys);
}