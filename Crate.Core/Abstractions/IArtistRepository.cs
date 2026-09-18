using Crate.Core.Domain;

namespace Crate.Core.Abstractions;

public interface IArtistRepository
{
    Task<IReadOnlyList<TrackedArtist>> GetArtistsAsync();
    Task<TrackedArtist?> GetArtistByIdAsync(Guid id);
    Task<TrackedArtist?> GetArtistByMbidAsync(string mbid);
    Task<TrackedArtist?> GetArtistBySpotifyIdAsync(string spotifyId);
    Task AddArtistAsync(TrackedArtist artist);
    Task UpdateArtistAsync(TrackedArtist artist);
    Task RemoveArtistAsync(Guid id);
}