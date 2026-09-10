using System.Text.Json.Serialization;

namespace Crate.Core.Persistence;

internal record TrackedArtistDto(
    [property: JsonPropertyName("id")]        Guid Id,
    [property: JsonPropertyName("name")]      string Name,
    [property: JsonPropertyName("mbid")]      string? Mbid,
    [property: JsonPropertyName("spotifyId")] string? SpotifyId,
    [property: JsonPropertyName("addedUtc")]  DateTimeOffset AddedUtc);

internal record SeenReleasesDto(
    [property: JsonPropertyName("keys")] List<string> Keys);