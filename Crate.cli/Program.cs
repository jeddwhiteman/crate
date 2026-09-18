using System.ComponentModel.DataAnnotations;
using Amazon.Runtime.Telemetry;
using Crate.Core.Application;
using Crate.Core.Domain;
using Crate.Core.Persistence;
using Crate.Core.Sources;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder().AddUserSecrets("f6105a9d-3f2d-4b5b-bea4-29a362a02857").Build();

string Required(string key) => config[key] ?? throw new Exception($"{key} missing from user secrets");

var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
var blobs = new FileBlobStore(dataDir);
var repo = new JsonArtistRepository(blobs);
var seen = new JsonSeenReleaseRepository(blobs);
var musicBrainzClient = new MusicBrainzClient("1.0", Required("Crate:ContactEmail"));

var command = args.Length > 0 ? args[0].ToLowerInvariant() : "help";


switch (command)
{
    case "add": await AddArtistAsync(string.Join(' ', args.Skip(1)));
        break;
    case "list": await ListArtistsAsync();
        break;
    case "remove": await RemoveArtistAsync(string.Join(' ', args.Skip(1)));
        break;
    case "run": await RunAsync();
        break;
    default:
        Console.WriteLine("""
                          Commands:
                            add <artist name>     search MusicBrainz and track an artist
                            list                  show tracked artists
                            remove <artist name>  stop tracking
                            run                   find releases and write data/out.html
                          """);
        break; 
}

return;

async Task AddArtistAsync(string artistName)
{
    if (string.IsNullOrWhiteSpace(artistName))
    {
        Console.WriteLine("Usage: add <artist name>");
        return;
    }
    
    var candidates = await musicBrainzClient.SearchArtistsAsync(artistName);

    if (candidates.Count == 0)
    {
        Console.WriteLine("No such artist found my g.");
        return;
    }
    
    for(var i = 0; i < candidates.Count; i++)
        Console.WriteLine($"{i + 1}. {candidates[i]}");

    if (!int.TryParse(Console.ReadLine(), out var pick) || pick < 1 || pick > candidates.Count) return;

    var chosen = candidates[pick - 1];

    if (await repo.GetArtistByMbidAsync(chosen.Mbid) is not null)
    {
        Console.WriteLine($"Already tracked.");
        return;
    }

    await repo.AddArtistAsync(new TrackedArtist(chosen.Name, chosen.Mbid));
    Console.WriteLine($"Added {chosen.Name}");
}

async Task ListArtistsAsync()
{
    var all = await repo.GetArtistAsync();
    Console.WriteLine($"{all.Count} tracked:");
    foreach (var trackedArtist in all)
        Console.WriteLine($"{trackedArtist}");
}

async Task RemoveArtistAsync(string artistName)
{
    var match =
        (await repo.GetArtistAsync()).FirstOrDefault(a => a.Name.Equals(artistName, StringComparison.OrdinalIgnoreCase));

    if (match is null)
    {
        Console.WriteLine("Artist not found g.");
        return;
    }

    await repo.RemoveArtistAsync(match.Id);
    Console.WriteLine($"Removed {match.Name}");
}

async Task RunAsync()
{
    var source = new MusicBrainzReleaseSource(musicBrainzClient);
    var result = await CrateRunner.RunAsync(source, repo, seen);

    var outPath = Path.Combine(dataDir, "out.html");
    Directory.CreateDirectory(dataDir);
    await File.WriteAllTextAsync(outPath, $"<html><body>{result.Html}</body></html>");

    Console.WriteLine($"\nWrote {outPath}");
    Console.WriteLine($"{result.FreshCount} new of {result.TotalFound} found.");
}