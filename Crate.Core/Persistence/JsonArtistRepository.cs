using System.Text.Json;
using Crate.Core.Abstractions;
using Crate.Core.Domain;

namespace Crate.Core.Persistence;

public class JsonArtistRepository(IBlobStore blobs, string key = "artists.json") : IArtistRepository
{
    public async Task<IReadOnlyList<TrackedArtist>> GetAllAsync()
    {
        var json = await blobs.ReadAsync(key);
        if (string.IsNullOrWhiteSpace(json)) return [];

        var dtos = JsonSerializer.Deserialize<List<TrackedArtist>>(json) ?? [];
        
        //return dtos.Select(ToDomain).ToList();
        return dtos;
    }

    public async Task<TrackedArtist?> GetByIdAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public async Task<TrackedArtist?> GetByMbidAsync(string mbid)
    {
        throw new NotImplementedException();
    }

    public async Task<TrackedArtist?> GetBySpotifyIdAsync(string spotifyId)
    {
        throw new NotImplementedException();
    }

    public async Task AddAsync(TrackedArtist artist)
    {
        throw new NotImplementedException();
    }

    public async Task UpdateAsync(TrackedArtist artist)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveAsync(Guid id)
    {
        throw new NotImplementedException();
    }
}