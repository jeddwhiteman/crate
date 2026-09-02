using Crate.Core.Domain;

namespace Crate.Core.Abstractions;

public interface IArtistRepository
{
    Task<IReadOnlyList<TrackedArtist>> GetAllAsync();
    Task<TrackedArtist?> GetByIdAsync(Guid id);
    Task<TrackedArtist?> GetByMbidAsync(string mbid);
    Task<TrackedArtist?> GetBySpotifyIdAsync(string spotifyId);
    Task AddAsync(TrackedArtist artist);
    Task UpdateAsync(TrackedArtist artist);
    Task RemoveAsync(Guid id);
}