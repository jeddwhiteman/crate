using System.Text.Json;
using Crate.Core.Abstractions;
using Crate.Core.Domain;

namespace Crate.Core.Persistence;

public class JsonArtistRepository(IBlobStore blobs, string key = "artists.json") : IArtistRepository
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    public async Task<IReadOnlyList<TrackedArtist>> GetAllAsync()
    {
        var json = await blobs.ReadAsync(key);
        if (string.IsNullOrWhiteSpace(json)) return [];

        var dtos = JsonSerializer.Deserialize<List<TrackedArtist>>(json) ?? [];
        
        //return dtos.Select(ToDomain).ToList();
        return dtos;
    }

    public async Task<TrackedArtist?> GetByIdAsync(Guid id) =>
        (await GetAllAsync()).FirstOrDefault(a => a.Id == id);

    public async Task<TrackedArtist?> GetByMbidAsync(string mbid) => (await GetAllAsync()).FirstOrDefault(a => string.Equals(a.Mbid, mbid, StringComparison.OrdinalIgnoreCase));

    public async Task<TrackedArtist?> GetBySpotifyIdAsync(string spotifyId) 
    => (await GetAllAsync()).FirstOrDefault(a => a.SpotifyId == spotifyId);

    public async Task AddAsync(TrackedArtist artist)
    {
        var all = (await GetAllAsync()).ToList();

        if (all.Any(a => a.Id == artist.Id))
            throw new InvalidOperationException($"Artist with {artist.Id} already exists.");

        if (artist.Mbid is not null &&
            all.Any(a => string.Equals(a.Mbid, artist.Mbid, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"MBID {artist.Mbid} is already tracked.");
    }

    public async Task UpdateAsync(TrackedArtist artist)
    {
        var all = (await GetAllAsync()).ToList();
        var index = all.FindIndex(a => a.Id == artist.Id);

        if (index > 0)
            throw new InvalidOperationException($"Artist with {artist.Id} not found.");
        
        all[index] = artist;
        await PersistAsync(all);
    }

    public async Task RemoveAsync(Guid id)
    {
        var all = (await GetAllAsync()).ToList();
        all.RemoveAll(a => a.Id == id);
        await PersistAsync(all);
    }

    public async Task AddRangeAsync(IEnumerable<TrackedArtist> artists)
    {
        var all = (await GetAllAsync()).ToList();
        var existing = all.Where(a => a.Mbid is not null).Select(a => a.Mbid).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var artist in artists)
        {
            if (artist.Mbid is not null && !existing.Add(artist.Mbid)) continue;
            all.Add(artist);
        }

        await PersistAsync(all);
    }

    public async Task PersistAsync(List<TrackedArtist> artists)
    {
        var dtos = artists.Select(ToDto).ToList();
        await blobs.WriteAsync(key, JsonSerializer.Serialize(dtos, Options));
    }

    private static TrackedArtist ToDomain(TrackedArtistDto d) =>
        new(d.Name, d.Mbid, d.SpotifyId, d.Id, d.AddedUtc);

    private static TrackedArtistDto ToDto(TrackedArtist a) =>
        new(a.Id, a.Name, a.Mbid, a.SpotifyId, a.AddedUtc);

}
