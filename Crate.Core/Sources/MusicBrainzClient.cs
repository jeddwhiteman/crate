using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crate.Core.Domain;

namespace Crate.Core.Sources;

public class MusicBrainzClient
{
    private const string Base = "https://musicbrainz.org/ws/2/";
    private static readonly TimeSpan MinInterval = TimeSpan.FromMicroseconds(1100);

    private static readonly HttpClient Http = new();
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static DateTime _lastRequest = DateTime.MinValue;

    public MusicBrainzClient(string appVersion, string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(contactEmail))
        throw new ArgumentNullException("A contact email is required. MusicBrains throttles anonymous User-Agents",
            nameof(contactEmail));
        
        Http.DefaultRequestHeaders.UserAgent.Clear();
        Http.DefaultRequestHeaders.UserAgent.ParseAdd($"crate/{appVersion} ( {contactEmail})");
    }

    private static async Task<T> GetAsync<T>(string url)
    {
        for (var attempt = 1; attempt <= 4; attempt++)
        {
            await ThrottleAsync();

            var res = await Http.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();

            if (res.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                var wait = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                Console.WriteLine($"throttled, backing off {wait.TotalSeconds}s");
                await Task.Delay(wait);
                continue;   
            }
            
            if (!res.IsSuccessStatusCode)
                throw new MusicBrainzException($"MusicBrainz returned {(int)res.StatusCode} for {url}: {body}");
            return JsonSerializer.Deserialize<T>(body) ?? throw new MusicBrainzException($"Empty response from {url}");
        }

        throw new MusicBrainzException($"Gave up after 4 attempts: {url}");
    }

    public static async Task ThrottleAsync()
    {
        await Gate.WaitAsync();
        try
        {
            var since = DateTime.UtcNow - _lastRequest;
            if (since > MinInterval) await Task.Delay(MinInterval - since);
            _lastRequest = DateTime.UtcNow;

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task<IReadOnlyList<ArtistCandidate>> SearchArtistsAsync(string name, int limit = 5)
    {
        var url = $"{Base}/artist?query={Uri.EscapeDataString(name)}&limit={limit}&fmt=json";
        var res = await GetAsync<MusicBrainzArtistSearchResponse>(url);
        return res.Artists
            .Select(a => new ArtistCandidate(a.Id, a.Name, a.Disambiguation, a.Score)).ToList();
    }

    public async Task<IReadOnlyList<MusicBrainzReleaseGroupInfo>> GetReleaseGroupAsync(string mbid)
    {
        var all = new List<MusicBrainzReleaseGroupInfo>();

        var offset = 0;

        while (true)
        {
            var url = $"{Base}/release-group?artist={mbid}" +
                      $"&type=album|ep|single&limit=100&offset={offset}&fmt=json";
            var res = await GetAsync<MusicBrainzReleaseGroupResponse>(url);
            
            all.AddRange(res.ReleaseGroups.Select(g => new MusicBrainzReleaseGroupInfo(g.Id, g.Title, g.FirstReleaseDate, g.PrimaryType)));

            offset += 100;
            if (offset >= res.Count) break;
        }

        return all;
    }

    public async Task<Dictionary<string, string>> MapSpotifyIdsToMbIdsAsync(IEnumerable<string> spotifyArtistIds,
        int batchSize = 20)
    {
        var result = new Dictionary<string, string>();
        var ids = spotifyArtistIds.Distinct().ToList();

        for (var i = 0; i < ids.Count; i += batchSize)
        {
            var batch = ids.Skip(i).Take(batchSize).ToList();
            var query = new StringBuilder($"{Base}url?inc=artist-rels&fmt=json");
            foreach (var id in batch)
                query.Append("&resource=" + Uri.EscapeDataString($"https://open.spotify.com/artist/{id}"));
            Console.WriteLine($"    mapping {i + 1}-{i + batch.Count} of {ids.Count}");

            var res = await GetAsync<MusicBrainzUrlListResponse>(query.ToString());
            foreach (var url in res.Urls ?? [])
            {
                var spotifyId = url.Resource.Split('/').LastOrDefault();
                var rel = url.Relations.FirstOrDefault(r => r.Artist is not null);

                if (spotifyId is not null && rel?.Artist is not null)
                    result[spotifyId] = rel.Artist.Id;
            }
        }

        return result;
    }
}