using Crate.Core.Abstractions;
using Crate.Core.Domain;

namespace Crate.Core.Application;

public class ReleaseFinder
{
    public static async Task<Dictionary<string, Release>> FindAsync(IReleaseSource source,
        IReadOnlyList<TrackedArtist> artists, int pastDays, int futureDays)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = today.AddDays(-pastDays);
        var to = today.AddDays(futureDays);
        var found = new Dictionary<string, Release>();

        for (var i = 0; i < artists.Count; i++)
        {
            var artist = artists[i];

            if (!artist.IsResolved)
            {
                Console.WriteLine($"{i + 1}/{artists.Count} {artist.Name} - no MBID, less keep it pushing my g.");
                continue;
            }

            Console.WriteLine($"  {i + 1}/{artists.Count} {artist.Name}");;
        }

        return found;
    }
}