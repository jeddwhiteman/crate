using System.Text.Json.Serialization;

namespace Crate.Core.Sources;

internal record MusicBrainzArtistSearchResponse(
    [property: JsonPropertyName("artists")] List<MusicBrainzArtist> Artists);

internal record MusicBrainzArtist(
    [property: JsonPropertyName("id")]             string Id,
    [property: JsonPropertyName("name")]           string Name,
    [property: JsonPropertyName("disambiguation")] string? Disambiguation,
    [property: JsonPropertyName("score")]          int Score);
    
internal record MusicBrainzReleaseGroupResponse(
    [property: JsonPropertyName("release-groups")]      List<MusicBrainzReleaseGroup> ReleaseGroups,
    [property: JsonPropertyName("release-group-count")] int Count);

internal record MusicBrainzReleaseGroup(
    [property: JsonPropertyName("id")]                 string Id,
    [property: JsonPropertyName("title")]              string Title,
    [property: JsonPropertyName("first-release-date")] string? FirstReleaseDate,
    [property: JsonPropertyName("primary-type")]       string? PrimaryType);

internal record MusicBrainzUrlListResponse(
    [property: JsonPropertyName("urls")] List<MusicBrainzUrl>? Urls);

internal record MusicBrainzUrl(
    [property: JsonPropertyName("resource")]  string Resource,
    [property: JsonPropertyName("relations")] List<MusicBrainzRelation>? Relations);

internal record MusicBrainzRelation(
    [property: JsonPropertyName("artist")] MusicBrainzArtistRef? Artist);

internal record MusicBrainzArtistRef(
    [property: JsonPropertyName("id")]   string Id,
    [property: JsonPropertyName("name")] string Name);