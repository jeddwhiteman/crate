namespace Crate.Core.Domain;

/// A search hit shown to you when adding an artist. Never persisted.
public record ArtistCandidate(
    string Mbid,
    string Name,
    string? Disambiguation,
    int Score)
{
    public override string ToString() =>
        Disambiguation is { Length: > 0 }
            ? $"{Name} — {Disambiguation} (score {Score})"
            : $"{Name} (score {Score})";
}