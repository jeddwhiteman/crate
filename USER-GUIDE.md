# Crate — Feature Guide

Crate is a CLI tool for tracking artists and finding out when they drop new music. You tell it which artists to follow (matched against the MusicBrainz music database), and it's designed to check for new releases and hand you a shareable list.

## Index

- [0. Setup — Required Before First Use](#0-setup--required-before-first-use)
- [1. Add an Artist](#1-add-an-artist)
- [2. List Tracked Artists](#2-list-tracked-artists)
- [3. Remove an Artist](#3-remove-an-artist)
- [4. Find New Releases (`run`)](#4-find-new-releases-run) — ⚠️ not functional yet
- [5. Local Data Storage](#5-local-data-storage)
- [6. Spotify Artist Linking](#6-spotify-artist-linking) — 🚧 built but not wired up
- [7. Docker Packaging](#7-docker-packaging)
- [8. AWS Lambda Project](#8-aws-lambda-project) — 🚧 scaffold only, no code
- [How It All Fits Together](#how-it-all-fits-together)

---

## 0. Setup — Required Before First Use

**What it does:** Crate must identify itself to MusicBrainz with a contact email (MusicBrainz's policy for API access). Without this, the app refuses to start.

**How to use it (CLI, one-time setup):**
```bash
cd Crate.cli
dotnet user-secrets set "Crate:ContactEmail" "you@example.com"
```
This is stored via .NET User Secrets (not in any file checked into source control), keyed by the `UserSecretsId` in `Crate.cli.csproj`. Every command below requires this to be set first.

---

## 1. Add an Artist

**What it lets a business user do:** search MusicBrainz's artist database by name and start "tracking" one, so it later shows up when you check for new releases.

**How to use it (CLI):**
```bash
dotnet run -- add "Freddie Gibbs"
```
- The app searches MusicBrainz and prints numbered candidates (name, disambiguation note, and a match-confidence score) — useful when multiple artists share a name.
- You type the number of the correct match.
- The app checks it isn't already tracked, then saves it locally.

```
1. Freddie Gibbs (score 100)
2. Freddie Gibbs & Madlib (score 85)
> 1
Added Freddie Gibbs
```

---

## 2. List Tracked Artists

**What it lets a business user do:** see everything currently being tracked, at a glance.

**How to use it (CLI):**
```bash
dotnet run -- list
```
```
2 tracked:
Freddie Gibbs (a74b1b7f-71d5-...)
Tyler, The Creator (unresolved)
```
An artist shows `(unresolved)` if it was added without a successful MusicBrainz match — that artist won't produce results in the release-finding feature (see [Section 4](#4-find-new-releases-run)).

---

## 3. Remove an Artist

**What it lets a business user do:** stop tracking an artist you no longer care about.

**How to use it (CLI):**
```bash
dotnet run -- remove "Freddie Gibbs"
```
Matches by exact name (case-insensitive). If no match is found, it tells you `Artist not found` and does nothing.

---

## 4. Find New Releases (`run`)

**What it's meant to let a business user do:** the core payoff feature — scan every tracked artist's MusicBrainz history for releases from the past week and the next ~2 months, skip anything you've already seen, and produce an HTML summary (`data/out.html`) you could email or share, grouped into "Out now" and "Coming soon."

**How to use it (CLI):**
```bash
dotnet run -- run
```

**⚠️ Current status: not functional.** The command is wired up and runs without crashing, but the piece that actually fetches releases per artist (`ReleaseFinder.FindAsync`) is an unfinished stub — it prints progress per tracked artist but never calls out to MusicBrainz for their release history, so it always reports **0 releases found**, regardless of what's tracked. The output file is still written, but it will always say "Nothing new this week."

```
Tracking 2 artists via MusicBrainz.
  1/2 Freddie Gibbs
  2/2 Tyler, The Creator - no MBID, skipping
Found  0 releases.
0 not seen before.

Wrote data/out.html
0 new of 0 found.
```
This is a real gap, not a config issue — the fix is a development task (finishing `ReleaseFinder.FindAsync` to call `IReleaseSource.GetReleasesAsync` per artist and populate the results), not something a business user can work around from the CLI.

---

## 5. Local Data Storage

**What it lets a business user do (implicitly):** nothing you interact with directly, but it's what makes the other commands persistent across runs.

Crate stores everything as JSON files in a `data/` folder created next to wherever you run the CLI from:

| File | Created by | Holds |
|---|---|---|
| `data/artists.json` | [Add](#1-add-an-artist) / [Remove](#3-remove-an-artist) | Your tracked-artist list |
| `data/seen.json` | [Run](#4-find-new-releases-run) | Which releases you've already been shown, so re-running doesn't repeat old news (last 500 remembered) |
| `data/out.html` | [Run](#4-find-new-releases-run) | The generated release summary |

You never create or edit these by hand — they're created automatically the first time something is written.

---

## 6. Spotify Artist Linking

**What it's meant to let a business user do:** link a tracked artist to their Spotify ID, so the release summary can include a direct Spotify link alongside each release.

**Current status: built, not exposed.** The plumbing exists —
- `TrackedArtist` has a `SpotifyId` field and a `LinkToSpotify(...)` method.
- `MusicBrainzClient.MapSpotifyIdsToMbIdsAsync` can batch-resolve Spotify artist IDs to MusicBrainz IDs.

— but there's no CLI command that calls any of this. A business user can't link a Spotify ID to a tracked artist today; it would need a new command (e.g. a future `link-spotify` command) to be added.

---

## 7. Docker Packaging

**What it lets a business user (or an ops person) do:** run Crate as a container instead of needing a local .NET install.

**How to use it:**
```bash
docker build -f Crate.cli/Dockerfile -t crate-cli .
docker run --rm crate-cli list
```
Note: the container still needs the MusicBrainz contact email configured — User Secrets are a local dev mechanism, so a containerized run would need that value supplied another way (e.g. an environment variable wired into configuration), which isn't set up yet.

---

## 8. AWS Lambda Project

**What it's meant to let a business user do:** eventually run Crate's release-check on a schedule in the cloud (e.g. a daily Lambda that emails a digest), based on the AWS SDK packages already referenced — S3 (storage), SES (email sending), and Systems Manager Parameter Store (config).

**Current status: empty scaffold.** The `Crate.Lambda` project has its dependencies declared in `Crate.Lambda.csproj` but contains no source code yet. It's a placeholder for future work, not a usable feature today.

---

## How It All Fits Together

```
 CLI command (Program.cs)
        │
        ├─ add/list/remove ──► IArtistRepository (JsonArtistRepository) ──► data/artists.json
        │
        └─ run ──► CrateRunner
                     ├─ reads tracked artists (IArtistRepository)
                     ├─ ReleaseFinder ──► IReleaseSource (MusicBrainzReleaseSource) ──► MusicBrainzClient ──► MusicBrainz API
                     ├─ dedupes against ISeenReleaseRepository (JsonSeenReleaseRepository) ──► data/seen.json
                     └─ HtmlRenderer ──► data/out.html
```

- **Everything is CLI-driven** — there's no web UI or API server; `Crate.cli` is the only entry point today.
- **Crate.Core** holds all the actual logic (domain models, MusicBrainz client, storage, release-finding) — `Crate.cli` is a thin wrapper that just parses commands and calls into it.
- **Crate.Lambda** is a separate, not-yet-implemented deployment target referenced by `Crate.cli` but doing nothing yet.
- The realistic end-to-end flow today is: **set up contact email → add artists → list to confirm → (run, once finished, to get a release digest) → remove when you lose interest.**
