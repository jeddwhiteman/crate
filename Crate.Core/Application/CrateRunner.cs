using System.Globalization;
using Crate.Core.Abstractions;

namespace Crate.Core.Application;

public class CrateRunner
{
    public static async Task<RunResult> RunAsync(IReleaseSource source, IArtistRepository artists,
        ISeenReleaseRepository seen, int pastDays = 7, int futureDays = 60)
    {
        var tracked = await artists.GetArtistsAsync();
        Console.WriteLine($"Tracking {tracked.Count} artists via {source.SourceName}.");

        var found = await ReleaseFinder.FindAsync(source, tracked, pastDays, futureDays);
        Console.WriteLine($"Found  {found.Count} releases.");

        var seenKeys = await seen.GetAllKeysAsync();
        var fresh = found.Where(kv => !seenKeys.Contains(kv.Key))
            .Select(kv => kv.Value)
            .ToList();

        Console.WriteLine($"{fresh.Count} not seen before.");

        var html = HtmlRenderer.Render(fresh);
        await seen.MarkSeenAsync(found.Keys);

        return new RunResult(found.Count, fresh.Count, html);
    }
}