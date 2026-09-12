using Crate.Core.Abstractions;
using Crate.Core.Domain;

namespace Crate.Core.Sources;

public class MusicBrainzReleaseSource(MusicBrainzClient client) : IReleaseSource
{
    public string SourceName => "MusicBrainz";

    public static DateOnly? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Length != 10) return null;
        return DateOnly.TryParse(raw, out var date) ? date : null;
    }
    
    public async Task<IReadOnlyList<Release>> GetReleasesAsync(TrackedArtist artist)
    {
        if (!artist.IsResolved) return [];

        var groups = await client.GetReleaseGroupAsync(artist.Mbid!);
        
        return groups
            .Select(g => new {Group = g, Date = ParseDate(g.FirstReleaseDate)})
            .Where(x => x.Date is not null)
            .Select(x => new Release(
                SourceId: x.Group.Id,
                ArtistName: artist.Name,
                Title: x.Group.Title,
                Date: x.Date!.Value,
                Type: x.Group.PrimaryType,
                Url: $"https://musicbrainz.org/release-group/{x.Group.Id}",
                SpotifyUrl: artist.SpotifyUrl))
            .ToList();
    }
}