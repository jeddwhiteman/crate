using Crate.Core.Domain;

namespace Crate.Core.Abstractions;

public interface IReleaseSource
{
    string SourceName { get; }
    Task<IReadOnlyList<Release>> GetReleasesAsync(TrackedArtist artist);
}