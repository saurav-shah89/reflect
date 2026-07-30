# Reflect

A journaling app for Windows. You write one entry a day in Markdown, pick how you felt,
tag it, and the app works out streaks and some stats from that. Everything is stored
locally in a SQLite file - nothing is uploaded anywhere.

Made with .NET MAUI Blazor Hybrid for CS6004NI Application Development, Coursework 1
(Islington College / London Metropolitan University).

## Features

1. One journal entry per day, which you can create, edit and delete
2. Markdown for writing, with a live preview next to the editor
3. Mood tracking - one main mood plus up to two extra ones
4. Tags, both the built-in list and any you type in yourself
5. Calendar view to browse and jump to a day
6. Timeline of all entries, paginated
7. Search by title and content, filter by date, mood, tag or category
8. Streaks - current, longest, and which days were missed
9. Light and dark themes
10. Dashboard with mood distribution, top tags, and word count over time
11. Passphrase or PIN lock
12. Export a date range to PDF

## Tech used

- C# / .NET 10
- .NET MAUI Blazor Hybrid
- MudBlazor 9.7.0 for the UI components
- SQLite through sqlite-net-pcl 1.11.285
- Markdig 1.3.2 for the Markdown
- QuestPDF 2026.7.1 (Community licence) for the PDF export

Windows is the target I built and tested against. The Android, iOS and Mac Catalyst
targets are set up in the csproj but I haven't run them - iOS and Mac need a Mac to
build at all.

### Why the SQLite native library is pinned

`SQLitePCLRaw.bundle_green` 2.1.11 brings in the native `e_sqlite3` libraries at 2.1.11,
which have a known vulnerability ([GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)).
There's no fixed version of the bundle yet, so the csproj references the native libraries
directly at 2.1.12, which overrides the version the bundle asks for. That's why restore
comes back with no NU1903 warnings.

## Running it

You need the .NET 10 SDK and the MAUI workloads:

```bash
dotnet workload install maui
```

Then:

```bash
git clone https://github.com/saurav-shah89/reflect.git
cd reflect
dotnet restore
dotnet build Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0
dotnet run --project Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0
```

The database is made automatically the first time it runs.

To publish a Release build:

```bash
dotnet publish Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0 -c Release
```

Release starts in about 4 seconds. The first run straight after a build is slower
because the WebView2 profile lives in the build folder and gets wiped when you rebuild.

## How it's put together

Two projects:

- **Reflect.Core** - the models, services and database code. Targets plain `net10.0`, no
  MAUI reference at all.
- **Reflect** - the MAUI app. Pages, layout, theme and the DI setup.

Splitting them meant `Reflect.Core` doesn't need MAUI. The one thing that did need it was
asking for the app data folder, so the path is passed into `JournalDatabase` from
`MauiProgram` instead.

```
Reflect.Core/
├── Data/           JournalDatabase, SeedData
├── Models/         entities, EntryQuery, PagedResult, analytics models
└── Services/       EntryService, AnalyticsService, SettingsService,
                    ReferenceDataService, MarkdownRenderer, PdfJournalExporter

Reflect/
├── Components/
│   ├── Layout/     MainLayout, NavMenu, LockScreen
│   ├── Pages/      Home, Write, Calendar, Journal, Dashboard, Export, Settings
│   └── Shared/     ScreenHeader, StatCard
├── Theme/          AppTheme
├── wwwroot/        app.css, index.html
└── MauiProgram.cs  service registration
```

The pages only ever talk to interfaces (`IEntryService` and so on), which are registered
in `MauiProgram.cs`. `JournalDatabase` is a singleton because it holds one connection and
only builds the schema once; the services are scoped.

## Database

Created on first run as `reflect.db3` in the app data folder.

```
entries
├── Id                  INTEGER  PK
├── EntryDate           DATETIME UNIQUE  <- one entry per day
├── Title               TEXT     max 200
├── Content             TEXT     the Markdown as typed
├── CreatedAt           DATETIME
├── UpdatedAt           DATETIME
├── PrimaryMoodId       INTEGER  required
├── SecondaryMoodOneId  INTEGER  nullable
├── SecondaryMoodTwoId  INTEGER  nullable
├── CategoryId          INTEGER  nullable
└── WordCount           INTEGER  stored so the chart doesn't recount every time

moods         15 rows, seeded    Id, Name, Category, SortOrder
tags          31 rows, seeded    Id, Name, IsCustom
categories    6 rows, seeded     Id, Name
entry_tags    join table         Id, EntryId, TagId  (unique together)
app_settings  one row only       Id, PasswordHash, PasswordSalt, Iterations,
                                 IsLockEnabled, Theme
```

The unique index on `EntryDate` is what actually enforces one entry per day, rather than
leaving it to the C# code.

Seeded moods, five per category:

| Category | Moods |
| --- | --- |
| Positive | Happy, Excited, Relaxed, Grateful, Confident |
| Neutral | Calm, Thoughtful, Curious, Nostalgic, Bored |
| Negative | Sad, Angry, Stressed, Lonely, Anxious |

To reset everything, close the app and delete `reflect.db3` - it gets rebuilt and reseeded
next time.

## Security

You can lock the journal with a passphrase or PIN from Settings.

The passphrase isn't stored. What's saved is a PBKDF2-HMAC-SHA256 hash, a random salt, and
the iteration count used, so opening `reflect.db3` in a SQLite browser tells you nothing
about it. I used 600,000 iterations, which is what OWASP recommends - it's about 90ms to
unlock. Comparison uses `CryptographicOperations.FixedTimeEquals` so how long it takes
doesn't hint at how much of the hash was right.

When it's locked the lock screen replaces the whole layout instead of sitting on top of
it, so nothing from the journal is rendered behind it. Nothing renders at all until the
lock state has been read, so the contents can't flash up on the way to the prompt. If the
settings row can't be read the app assumes it's locked, since guessing the other way would
expose the journal.

Changing or removing the passphrase asks for the current one even if you're already
unlocked, so someone can't just take the lock off a machine you left open.

Worth being clear about what it doesn't do:

- The database file itself isn't encrypted. The lock is on the app, not the file - anyone
  with the file and a SQLite client can read it. Encrypting it would need something like
  SQLCipher, which is more than the spec asks for.
- There's no way to recover a forgotten passphrase. Adding one would defeat the point.
- Failed attempts are counted and shown but not rate limited.

## Problems I ran into

**App closes immediately with `0xC000027B`, or `REGDB_E_CLASSNOTREG`, or
`0x80073CFF` saying you need a developer license.** This is Windows Developer Mode being
off. Rather than turn it on, the csproj sets `WindowsPackageType=None` so it builds
unpackaged and doesn't need it.

**`NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64`.** MAUI asks
for a Mono runtime pack for Windows that doesn't exist. Add `-p:UseMonoRuntime=false` to
the build.

**`NETSDK1005: Assets file doesn't have a target for net10.0-windows...`.** I got this
passing `-f net10.0-windows...` to the whole solution. `Reflect.Core` targets plain
`net10.0`, so the framework flag has to go on `Reflect.csproj` and not the solution.

**Changes not showing up.** MAUI can leave stale output behind. `dotnet clean` then
rebuild, or delete `bin` and `obj`.

**"You cannot write an entry for a future date."** Working as intended - an entry records
a day that's actually happened.
