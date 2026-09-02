namespace Crate.Core.Domain;

public record Release(
    string SourceId,
    string ArtistName,
    string Title,
    DateOnly Date,
    string? Type,
    string? Url,
    string? SpotifyUrl)
{
    public bool IsUpcoming(DateOnly today) => Date > today;


    public string SeenKey(DateOnly today) =>
        $"{SourceId}:{(IsUpcoming(today) ? "upcoming" : "released")}";
}