namespace Crate.Core.Domain;

public class TrackedArtist
{
    public Guid Id { get; }
    public string Name { get; private set; }
    public string? Mbid { get; private set; }
    public string? SpotifyId { get; private set; }
    public DateTimeOffset AddedUtc { get; }

    public TrackedArtist(
        string name,
        string? mbid = null,
        string? spotifyId = null,
        Guid? id = null,
        DateTimeOffset? addedUtc = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Artist name is required.", nameof(name));

        Id        = id ?? Guid.NewGuid();
        Name      = name.Trim();
        Mbid      = mbid;
        SpotifyId = spotifyId;
        AddedUtc  = addedUtc ?? DateTimeOffset.UtcNow;
    }

    public bool IsResolved => !string.IsNullOrWhiteSpace(Mbid);

    public string? SpotifyUrl =>
        SpotifyId is null ? null : $"https://open.spotify.com/artist/{SpotifyId}";

    public void LinkToMusicBrainz(string mbid)
    {
        if (string.IsNullOrWhiteSpace(mbid))
            throw new ArgumentException("MBID is required.", nameof(mbid));
        Mbid = mbid;
    }

    public void LinkToSpotify(string spotifyId)
    {
        if (string.IsNullOrWhiteSpace(spotifyId))
            throw new ArgumentException("Spotify id is required.", nameof(spotifyId));
        SpotifyId = spotifyId;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Artist name is required.", nameof(name));
        Name = name.Trim();
    }

    public override bool Equals(object? obj) =>
        obj is TrackedArtist other && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString() =>
        $"{Name} ({(IsResolved ? Mbid : "unresolved")})";
}