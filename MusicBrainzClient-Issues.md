# MusicBrainzClient.cs — Issue Report

Scope: `Crate.Core/Sources/MusicBrainzClient.cs` and its two call sites (`Crate.cli/Program.cs`, `Crate.Core/Sources/MusicBrainzDtos.cs`).

Two categories are covered, per request:
- **Category A — Business process blockers**: bugs that stop a feature from working at all.
- **Category B — MusicBrainz policy / compliance risks**: things that could get the app's IP or User-Agent throttled or banned by MusicBrainz.

---

## Category A — Business Process Blockers

### 1. The `run` command is documented but not wired up — the release-finding workflow can never execute

**Description**
`Program.cs` prints `run` as a supported command in its help text and defines a full `RunAsync()` local function to do the work (find new releases, write `data/out.html`). But the `switch` statement that dispatches CLI commands has no `case "run":` branch. The compiler even flags this as warning `CS8321: The local function 'RunAsync' is declared but never used`. Typing `dotnet run -- run` silently falls through to the `default` case and just reprints the help text — the entire "find releases" business process is unreachable.

**Visual example**
```
$ dotnet run -- run
Commands:
  add <artist name>     search MusicBrainz and track an artist
  list                  show tracked artists
  remove <artist name>  stop tracking
  run                   find releases and write data/out.html
```
Nothing else happens — no HTML is written, no releases are found, despite the help text advertising it.

```csharp
// Program.cs — switch statement
switch (command)
{
    case "add": await AddArtistAsync(...); break;
    case "list": await ListArtistsAsync(); break;
    case "remove": await RemoveArtistAsync(...); break;
    // no case "run" — RunAsync() is never called
    default: Console.WriteLine("""...help text..."""); break;
}
```

**Potential fixes**
- Add `case "run": await RunAsync(); break;` to the switch statement.
- Add a test/manual check that every command in the help text has a matching `case`.

---

### 2. Missing `/` separator breaks the Spotify → MusicBrainz ID mapping URL

**Description**
`Base` is `"https://musicbrainz.org/ws/2"` (no trailing slash). Two call sites correctly prepend their own `/` (`$"{Base}/artist?..."`, `$"{Base}/release-group?..."`), but `MapSpotifyIdsToMbIdsAsync` builds its URL as `$"{Base}url?..."` — with no slash at all. This is currently dead code (see Issue 4), but it's a real defect that will break the moment the Spotify-mapping feature is called.

**Visual example**
```
Intended : https://musicbrainz.org/ws/2/url?inc=artist-rels&fmt=json
Actual   : https://musicbrainz.org/ws/2url?inc=artist-rels&fmt=json
                                        ^^^ missing "/"
```
```csharp
// MusicBrainzClient.cs:112
var query = new StringBuilder($"{Base}url?inc=artist-rels&fmt=json");
// should be:
var query = new StringBuilder($"{Base}/url?inc=artist-rels&fmt=json");
```
MusicBrainz would respond `404 Not Found` to the malformed path, exactly like the earlier `add` crash.

**Potential fixes**
- Add the missing `/` before `url?...`.
- Better: normalize `Base` to always end in `/` and drop the leading `/` from every call site (or vice versa), so this class of bug can't recur — right now three call sites each hand-manage the slash independently.

---

### 3. Null-reference crash risk when a MusicBrainz URL resource has no relations

**Description**
`MusicBrainzUrl.Relations` is declared nullable (`List<MusicBrainzRelation>? Relations`), because MusicBrainz can return a URL resource with no `relations` array at all. `MapSpotifyIdsToMbIdsAsync` calls `url.Relations.FirstOrDefault(...)` directly with no null check — this is exactly what the compiler's `CS8604` warning is pointing at. If MusicBrainz ever returns a URL entry without relations, this throws `NullReferenceException` and crashes the whole batch, taking down the mapping for every other artist in that batch too.

**Visual example**
```json
// A real possible MusicBrainz response for one url entry:
{ "resource": "https://open.spotify.com/artist/abc123", "relations": null }
```
```csharp
// MusicBrainzClient.cs:120-121
var spotifyId = url.Resource.Split('/').LastOrDefault();
var rel = url.Relations.FirstOrDefault(r => r.Artist is not null);
//        ^^^^^^^^^^^^^ NullReferenceException if Relations is null
```

**Potential fixes**
- `url.Relations?.FirstOrDefault(r => r.Artist is not null)` and let `rel` be null (the `rel?.Artist is not null` check two lines down already handles a null `rel`).

---

### 4. `RunAsync` writes to a directory path, not a file — will crash the moment Issue 1 is fixed

**Description**
Inside the (currently unreachable) `RunAsync`, `outPath` is built as a **file** path (`.../data/out.html`), but the code calls `Directory.CreateDirectory(outPath)` on it — creating a *folder* literally named `out.html`. The very next line then tries `File.WriteAllTextAsync(outPath, ...)` on that same path, which fails because a directory already occupies it. This will surface as soon as Issue 1 (wiring up `run`) is fixed, so it should be fixed at the same time.

**Visual example**
```csharp
// Program.cs:104-106
var outPath = Path.Combine(dataDir, "out.html");
Directory.CreateDirectory(outPath);              // creates a FOLDER named "out.html"
await File.WriteAllTextAsync(outPath, ...);      // fails: "out.html" is a directory, not a file
```
```
Unhandled exception. System.UnauthorizedAccessException / IOException:
Access to the path '.../data/out.html' is denied. (it's a directory)
```

**Potential fixes**
- `Directory.CreateDirectory(dataDir)` (ensure the *parent* folder exists), not `outPath` itself.

---

## Category B — MusicBrainz Policy / Compliance Risks

### 5. Rate limiter is ~1000x too fast — real risk of an IP/User-Agent ban from MusicBrainz

**Description**
MusicBrainz's usage policy caps unauthenticated/lightly-used clients at **1 request per second**. The app's throttle window is set with `TimeSpan.FromMicroseconds(1100)` — that's **1.1 milliseconds**, not 1.1 seconds. The throttle logic itself (`since < MinInterval`) is now correct, but the interval it's enforcing is roughly 1,000 times shorter than MusicBrainz requires. In practice this means the app can fire off requests as fast as .NET's HTTP stack allows, which is a direct policy violation and a plausible trigger for MusicBrainz to block the app's IP address or User-Agent string.

**Visual example**
```csharp
// MusicBrainzClient.cs:12
private static readonly TimeSpan MinInterval = TimeSpan.FromMicroseconds(1100); // = 0.0011 seconds
// MusicBrainz policy requires >= 1 request/second, i.e. MinInterval should be ~1 second
```
```
Intended pacing:  |request|----------1.0s----------|request|----------1.0s----------|request|
Actual pacing:    |request|-1.1ms-|request|-1.1ms-|request|-1.1ms-|request|-1.1ms-|request| ...
```

**Potential fixes**
- Change to `TimeSpan.FromMilliseconds(1100)` (a little over MusicBrainz's documented 1 req/sec minimum, matching what the variable name and surrounding retry logic clearly intended).

---

### 6. Backoff on throttling ignores any server-provided `Retry-After` guidance

**Description**
When MusicBrainz responds `503 Service Unavailable` (its "you're going too fast" signal), the code always backs off using a fixed formula (`2^attempt` seconds: 2s, 4s, 8s, 16s) rather than checking whether the response includes a `Retry-After` header telling the client exactly how long to wait. This isn't a violation on its own, but it's a best-practice gap: a fixed guess can under- or over-shoot what MusicBrainz actually wants, and repeatedly under-shooting a rate-limit signal is one of the more common ways clients get flagged.

**Visual example**
```csharp
// MusicBrainzClient.cs:37-42
if (res.StatusCode == HttpStatusCode.ServiceUnavailable)
{
    var wait = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // ignores res.Headers.RetryAfter
    Console.WriteLine($"throttled, backing off {wait.TotalSeconds}s");
    await Task.Delay(wait);
    continue;
}
```

**Potential fixes**
- Prefer `res.Headers.RetryAfter` when present, falling back to the exponential backoff only when the server doesn't specify a value.

---

## Summary Table

| # | Title | Category | Blocks business process? | Policy/ban risk? |
|---|---|---|---|---|
| 1 | `run` command never wired up | A | Yes — feature unreachable | No |
| 2 | Missing `/` in Spotify mapping URL | A | Yes — 404 when called | No |
| 3 | Null-ref risk on `Relations` | A | Yes — crash when triggered | No |
| 4 | `Directory.CreateDirectory(outPath)` on a file path | A | Yes — crash once #1 fixed | No |
| 5 | Rate limiter ~1000x too fast | B | No | **Yes — high** |
| 6 | Backoff ignores `Retry-After` | B | No | Minor |
