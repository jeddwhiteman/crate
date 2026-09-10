namespace Crate.Core.Sources;

public record MusicBrainzReleaseGroupInfo( 
    string Id,
    string Title,
    string? FirstReleaseDate,
    string? PrimaryType);