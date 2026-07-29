# Reflect

A secure, local-first journaling application for the desktop. Reflect lets you write one
entry per day in Markdown, track how you felt, tag and categorise what you wrote, and see
your habits emerge through streaks and dashboard analytics. All data stays on your machine
in a local SQLite database.

Built with .NET MAUI Blazor Hybrid for **CS6004NI Application Development**, Coursework 1
(Islington College / London Metropolitan University).

---

## Project Status

> **All twelve specified features are implemented.** This section is kept honest and current
> so nobody is misled about what runs today; anything not yet done is named as such.

| Area | Status |
| --- | --- |
| Project scaffold (.NET 10, four platforms) | Complete — builds and launches on Windows |
| Domain model (entries, moods, tags, categories, settings) | Complete |
| SQLite database, schema creation and reference-data seeding | Complete |
| `EntryService` — CRUD, search, paging | Complete |
| Reference data and Markdown services | Complete |
| Dependency injection and MudBlazor shell | Complete |
| Entry editor — write, moods, tags, category, delete | Complete |
| Calendar month view | Complete |
| Paginated journal with search and filters | Complete |
| Streaks and dashboard analytics | Complete |
| Journal lock (passphrase or PIN) | Complete |
| PDF export by date range | Complete |
| Theme persistence | Complete |
| Automated test project | None yet — see [Testing](#testing) |

What this means practically: you can write a Markdown entry for today or any past day, record
moods, tag and categorise it, browse a month at a glance in the calendar, page through
everything you have written, search or filter by text, date range, mood, tag and category, see
streaks and analytics on the dashboard, protect the journal with a passphrase or PIN, choose a
theme that persists, and export any date range to PDF.

What remains is not features but engineering polish — chiefly a committed test project. See
[Roadmap](#roadmap).

---

## Table of Contents

- [Key Features](#key-features)
- [Tech Stack](#tech-stack)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Available Commands](#available-commands)
- [Architecture](#architecture)
- [Database Schema](#database-schema)
- [Configuration](#configuration)
- [Security](#security)
- [Testing](#testing)
- [Building and Publishing](#building-and-publishing)
- [Troubleshooting](#troubleshooting)
- [Roadmap](#roadmap)
- [Coursework Mapping](#coursework-mapping)
- [Academic Integrity](#academic-integrity)

---

## Key Features

The twelve features required by the specification, with what works today marked. Anything not
marked done is a target, not a description of current behaviour.

| # | Feature | Description | Status |
| --- | --- | --- | --- |
| 1 | Journal entry management | Create, update and delete a single entry per calendar day, with system-generated timestamps | Done |
| 2 | Markdown writing | Formatting support — bold, italics, lists, headings, links | Done |
| 3 | Mood tracking | One required primary mood plus up to two optional secondary moods | Done |
| 4 | Tagging | Pre-built and user-created tags to classify entries | Done |
| 5 | Calendar navigation | Browse entries through a month view | Done |
| 6 | Paginated journal view | Timeline list, page by page | Done |
| 7 | Search and filter | Search title and content; filter by date range, moods, tags or category | Done |
| 8 | Streak tracking | Current streak, longest streak and missed days | Done |
| 9 | Theme customisation | Light and dark themes | Done |
| 10 | Dashboard analytics | Mood distribution, most frequent mood, most used tags, tag breakdown, word-count trends | Done |
| 11 | Security and privacy | Password or PIN protection for the journal | Done |
| 12 | Export | Export a date range of entries to PDF | Done |

### Design decisions already made

- **One entry per day is enforced by the database**, not just by application code. `entries`
  carries a unique index on `EntryDate`, so a bug or a race cannot produce two entries on the
  same day.
- **Secondary moods are two nullable columns**, not a join table. The specification caps them
  at two, so the schema enforces that limit for free.
- **The passphrase is never stored.** Only a PBKDF2-SHA256 hash and a per-install random salt
  are persisted, with the iteration count stored alongside so the work factor can be raised
  later without invalidating existing credentials.
- **Word count is stored on the entry** rather than recomputed, so the word-count-trend chart
  does not have to parse every entry body on load.

---

## Tech Stack

| Layer | Choice | Version |
| --- | --- | --- |
| Language | C# | `net10.0` |
| Runtime | .NET | 10.0 |
| App framework | .NET MAUI Blazor Hybrid | matches installed MAUI workload |
| UI components | MudBlazor | 9.7.0 |
| Persistence | SQLite via `sqlite-net-pcl` | 1.11.285 |
| SQLite native provider | `SQLitePCLRaw.bundle_green` | 2.1.11 |
| SQLite native library | `SQLitePCLRaw.lib.e_sqlite3` | 2.1.12 (pinned — see note) |
| Markdown rendering | Markdig | 1.3.2 |
| PDF generation | QuestPDF (Community licence) | 2026.7.1 |
| Logging | `Microsoft.Extensions.Logging` | 10.0.0 |

### Target platforms

`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`

Windows is the primary development and demonstration target. iOS and Mac Catalyst are
configured but cannot be built without a macOS build host.

### Note on the pinned SQLite native library

`SQLitePCLRaw.bundle_green` 2.1.11 transitively pulls the native `e_sqlite3` libraries at
2.1.11, which fall under [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)
(high severity; vulnerable range `<= 2.1.11`). No fixed `bundle_green` has shipped, so the
native libraries are pinned forward to 2.1.12 with direct `PackageReference` entries, which
take precedence over transitive versions. Restore is free of `NU1903` warnings as a result.

---

## Prerequisites

| Requirement | Notes |
| --- | --- |
| .NET SDK 10.0 or later | Verify with `dotnet --version`. Developed against 10.0.302 |
| .NET MAUI workloads | `android`, `ios`, `maccatalyst`, `maui-windows` |
| Windows 10 build 19041+ | Required for the Windows target |
| Visual Studio 2022 17.12+ or VS Code with C# Dev Kit | Optional — the CLI is sufficient |
| Android SDK + emulator or device | Only if building the Android target |
| A macOS build host | Only if building iOS or Mac Catalyst |

Install the MAUI workloads if you do not already have them:

```bash
dotnet workload install maui
```

Confirm what is installed:

```bash
dotnet workload list
```

Expected output includes `android`, `ios`, `maccatalyst` and `maui-windows`.

---

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/saurav-shah89/reflect.git
cd reflect
```

### 2. Restore dependencies

```bash
dotnet restore Reflect/Reflect.csproj
```

This pulls MudBlazor, sqlite-net-pcl, Markdig and the SQLite native libraries. A clean
restore should print no `NU1903` vulnerability warnings — if it does, the native library pin
described above has been lost.

### 3. Build

Build the Windows target:

```bash
dotnet build Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0
```

Or the Android target:

```bash
dotnet build Reflect/Reflect.csproj -f net10.0-android
```

Building without `-f` attempts every target framework at once, which will fail on Windows
because iOS and Mac Catalyst need a Mac. Always pass `-f` during development.

### 4. Run

```bash
dotnet build Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0 -t:Run
```

Or launch the built executable directly:

```bash
./Reflect/bin/Debug/net10.0-windows10.0.19041.0/win-x64/Reflect.exe
```

The app opens a desktop window. On first run it creates the SQLite database and seeds the
fixed reference data — 15 moods, 31 tags and 6 categories.

> **Note:** the Windows build is configured as **unpackaged**
> (`<WindowsPackageType>None</WindowsPackageType>`), which means it runs without requiring
> Windows Developer Mode or MSIX sideloading. This is deliberate; see
> [Troubleshooting](#troubleshooting).

---

## Available Commands

| Command | Description |
| --- | --- |
| `dotnet restore Reflect/Reflect.csproj` | Restore NuGet packages |
| `dotnet build Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0` | Build the Windows target |
| `dotnet build Reflect/Reflect.csproj -f net10.0-android` | Build the Android target |
| `dotnet build Reflect/Reflect.csproj -f <tfm> -t:Run` | Build and launch |
| `dotnet build Reflect/Reflect.csproj -f <tfm> -c Release` | Release build |
| `dotnet clean Reflect/Reflect.csproj` | Remove build outputs |
| `dotnet publish Reflect/Reflect.csproj -f <tfm> -c Release` | Produce a distributable build |
| `dotnet workload list` | Show installed MAUI workloads |
| `dotnet list Reflect/Reflect.csproj package --vulnerable --include-transitive` | Audit dependencies |

---

## Architecture

### Layering

Reflect is organised in three layers with dependencies pointing inward. Razor components
depend on service interfaces; service implementations depend on the database abstraction;
nothing depends on a concrete SQLite connection except `JournalDatabase` itself.

```
Components (Razor / MudBlazor)      presentation
        │  depends on
        ▼
Services.Interfaces  ←  Services    business logic
        │  depends on
        ▼
Data.IJournalDatabase  ←  Data      persistence
        │
        ▼
    SQLite file
```

This satisfies the specification's code-modularity criteria — separation of concerns, single
responsibility, abstraction and dependency injection — and means the storage mechanism can be
swapped (for example for an in-memory database in tests) without touching business logic.

### Directory structure

```
.
├── Reflect.sln                     Solution file
├── README.md                       This file
└── Reflect/
    ├── Reflect.csproj              Project file, target frameworks, packages
    ├── MauiProgram.cs              App composition root and DI registration
    ├── App.xaml(.cs)               MAUI application shell
    ├── MainPage.xaml(.cs)          Hosts the BlazorWebView
    │
    ├── Models/                     Domain entities and value types
    │   ├── JournalEntry.cs         One entry per day
    │   ├── Mood.cs                 A selectable mood
    │   ├── MoodCategory.cs         Positive / Neutral / Negative
    │   ├── Tag.cs                  Pre-built or custom label
    │   ├── Category.cs             Single grouping per entry
    │   ├── EntryTag.cs             Entry-to-tag join row
    │   ├── AppSettings.cs          Theme and credential hash
    │   ├── EntryQuery.cs           Search and filter criteria
    │   └── PagedResult.cs          One page of results plus totals
    │
    ├── Data/                       Persistence
    │   ├── IJournalDatabase.cs     Connection abstraction
    │   ├── JournalDatabase.cs      Connection, schema creation, seeding
    │   └── SeedData.cs             The specification's fixed reference lists
    │
    ├── Services/                   Business logic
    │   ├── Interfaces/
    │   │   └── IEntryService.cs    Entry CRUD and querying contract
    │   └── DuplicateEntryDateException.cs
    │
    ├── Components/                 Blazor UI
    │   ├── Routes.razor            Router
    │   ├── _Imports.razor          Global usings for components
    │   ├── Layout/                 Shell and navigation
    │   └── Pages/                  Routable pages
    │
    ├── Platforms/                  Per-platform entry points and manifests
    │   ├── Android/  iOS/  MacCatalyst/  Windows/
    │
    ├── Resources/                  App icon, splash, fonts, raw assets
    └── wwwroot/                    Static web assets served to the WebView
        ├── index.html
        └── app.css
```

### How MAUI Blazor Hybrid works here

1. `MauiProgram.CreateMauiApp()` builds the host and registers services.
2. `App` creates a window whose content is `MainPage`.
3. `MainPage` hosts a `BlazorWebView` pointed at `wwwroot/index.html`.
4. Razor components render **inside a native WebView**, but run as ordinary .NET code in the
   same process — there is no HTTP server and no JavaScript interop boundary for business
   logic.
5. Components resolve services through dependency injection and call straight into the
   service layer, which reads and writes the local SQLite file.

The practical consequence: this is a desktop app that happens to use web rendering. File
system and database access are direct .NET calls, not network requests.

### Database initialisation flow

`JournalDatabase.GetConnectionAsync()` is the single entry point to storage:

1. Fast path — return the cached connection if initialisation already completed.
2. Otherwise take a `SemaphoreSlim`, then re-check inside the lock so concurrent callers do
   not initialise twice.
3. Enable `PRAGMA foreign_keys = ON`.
4. Create tables for all six entities.
5. Seed moods, tags, categories and the settings row, each guarded by a count check so
   restarting never duplicates reference data.
6. Cache and return the connection.

---

## Database Schema

The database file is created on first run inside the platform's app-data directory as
`reflect.db3`.

```
entries                                 one row per calendar day
├── Id                  INTEGER  PK, autoincrement
├── EntryDate           DATETIME UNIQUE, not null   ← enforces one entry per day
├── Title               TEXT     max 200, not null
├── Content             TEXT     raw Markdown as typed
├── CreatedAt           DATETIME set once, never modified
├── UpdatedAt           DATETIME refreshed on every save
├── PrimaryMoodId       INTEGER  indexed, required
├── SecondaryMoodOneId  INTEGER  nullable
├── SecondaryMoodTwoId  INTEGER  nullable
├── CategoryId          INTEGER  indexed, nullable
└── WordCount           INTEGER  denormalised for analytics

moods                                   seeded, 15 rows
├── Id                  INTEGER  PK, autoincrement
├── Name                TEXT     UNIQUE, max 40
├── Category            INTEGER  indexed — 0 Positive, 1 Neutral, 2 Negative
└── SortOrder           INTEGER  display order within category

tags                                    seeded with 31 pre-built rows
├── Id                  INTEGER  PK, autoincrement
├── Name                TEXT     UNIQUE, max 40
└── IsCustom            INTEGER  0 pre-built, 1 user-created

categories                              seeded, 6 rows
├── Id                  INTEGER  PK, autoincrement
└── Name                TEXT     UNIQUE, max 40

entry_tags                              many-to-many join
├── Id                  INTEGER  PK, autoincrement
├── EntryId             INTEGER  ┐ composite UNIQUE index
└── TagId               INTEGER  ┘ prevents duplicate links

app_settings                            single row, Id always 1
├── Id                  INTEGER  PK
├── PasswordHash        TEXT     base64 PBKDF2-SHA256, empty until set
├── PasswordSalt        TEXT     base64 per-install random salt
├── Iterations          INTEGER  PBKDF2 work factor used
├── IsLockEnabled       INTEGER  1 once a passphrase is chosen
└── Theme               TEXT     "light" or "dark"
```

### Seeded reference data

**Moods** — fixed by the specification, five per category:

| Category | Moods |
| --- | --- |
| Positive | Happy, Excited, Relaxed, Grateful, Confident |
| Neutral | Calm, Thoughtful, Curious, Nostalgic, Bored |
| Negative | Sad, Angry, Stressed, Lonely, Anxious |

**Tags** — 31 pre-built: Work, Career, Studies, Family, Friends, Relationships, Health,
Fitness, Personal Growth, Self-care, Hobbies, Travel, Nature, Finance, Spirituality,
Birthday, Holiday, Vacation, Celebration, Exercise, Reading, Writing, Cooking, Meditation,
Yoga, Music, Shopping, Parenting, Projects, Planning, Reflection.

**Categories** — 6 defaults, extensible by the user: Personal, Work, Health, Travel,
Learning, Relationships.

### Inspecting the database

The file lives inside the platform app-data directory, resolved at runtime by
`FileSystem.AppDataDirectory`. The full path is exposed in code as
`JournalDatabase.DatabasePath` and is written to the log at startup:

```
Journal database ready at <path>
```

Open it with any SQLite client — the `sqlite3` CLI or DB Browser for SQLite both work.

To reset to a clean state, close the app and delete the file. It is recreated and reseeded on
next launch.

---

## Configuration

Reflect is a local desktop application. It has **no environment variables, no `.env` file,
no connection strings and no external services** — all state lives in the local SQLite file.

Configuration that does exist is expressed as MSBuild properties in `Reflect.csproj`:

| Property | Value | Purpose |
| --- | --- | --- |
| `TargetFrameworks` | `net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0` | Platforms built |
| `WindowsPackageType` | `None` | Unpackaged Windows build — runs without Developer Mode |
| `ApplicationId` | `com.companyname.reflect` | Platform bundle identifier |
| `ApplicationTitle` | `Reflect` | Window and installer title |
| `ApplicationDisplayVersion` / `ApplicationVersion` | `1.0` / `1` | Version shown to users / build number |
| `Nullable` | `enable` | Nullable reference types on |
| `ImplicitUsings` | `enable` | Common namespaces imported automatically |

Runtime constants worth knowing:

| Constant | Location | Value |
| --- | --- | --- |
| Database file name | `JournalDatabase.DatabaseFileName` | `reflect.db3` |
| Database full path | `JournalDatabase.DatabasePath` | `FileSystem.AppDataDirectory` + file name |
| Settings row id | `AppSettings.SingletonId` | `1` |

---

## Security

The journal can be locked with a passphrase or PIN, set from the Settings page.

**The passphrase is never stored.** What is persisted is a PBKDF2-HMAC-SHA256 hash, a random
per-install salt, and the iteration count used to produce it — so reading `reflect.db3`
reveals nothing about the credential, and the same passphrase on two installs produces
different hashes.

| Property | Choice | Why |
| --- | --- | --- |
| Algorithm | PBKDF2-HMAC-SHA256 | Available in the base class library; no extra dependency |
| Work factor | 600,000 iterations | OWASP guidance; measured at ~90ms per verification |
| Salt | 32 random bytes per install | Defeats precomputed tables and cross-install comparison |
| Comparison | `CryptographicOperations.FixedTimeEquals` | Verification time does not leak how much matched |
| Iteration count | Stored alongside the hash | The work factor can be raised later without invalidating existing passphrases |

When locked, the lock screen **replaces the entire layout** rather than overlaying it, so no
navigation and no entry content is rendered behind it. Nothing renders at all until the lock
state is known, so a locked journal cannot flash its contents on the way to the prompt. If the
settings row cannot be read at startup the app treats itself as locked — failing open would
expose the journal, failing closed costs only a prompt.

Changing or removing the passphrase requires the current one even when the session is already
unlocked, so an unattended machine cannot be used to lock the owner out.

### What this does not do

- **The database itself is not encrypted.** The lock gates the application, not the file.
  Anyone with access to `reflect.db3` and a SQLite client can read entries directly. Encrypting
  at rest would need SQLCipher or equivalent, which is beyond the specification's requirement
  for "password or PIN protection".
- **There is no recovery.** A forgotten passphrase cannot be reset, by design — a reset path
  reachable without the credential would defeat the lock.
- **There is no attempt throttling.** Failed attempts are counted and shown but not rate
  limited; the 600,000-iteration work factor is what makes bulk guessing expensive.

---

## Testing

**There is no test project in the repository yet.** This section documents the intended
approach rather than committed tests, so the gap is not mistaken for standing coverage.

The service layer has, however, been verified against real SQLite files using throwaway
harnesses that linked the actual service sources:

- **`EntryService` — 48 checks.** Insert, update, the transactional tag rewrite, the
  one-entry-per-day constraint, every search filter, paging edges and cascade-on-delete.
- **`AnalyticsService` — 47 checks.** Empty journals, streaks broken and at risk, runs
  spanning a month boundary, percentage totals, uncategorised grouping, all three trend
  granularities, reversed bounds, and empty ranges producing zeroes rather than NaN.
- **`SettingsService` — 41 checks.** Plaintext never reaching storage, per-install salts,
  wrong passphrases failing to disable the lock, corrupt credential material handled, and
  verification timing inside its intended window.
- **`PdfJournalExporter` — 13 checks.** Valid PDF structure, reversed and empty ranges,
  non-file streams, and null rejection.

All passed in full. Those harnesses live outside the repository, so they are point-in-time
results rather than a suite that runs on every build. Turning them into a committed test
project is item 12 on the [Roadmap](#roadmap).

The architecture is already set up to make testing straightforward: services depend on
`IJournalDatabase`, so a test can supply a connection to an in-memory SQLite database
(`":memory:"`) and exercise business logic with no file system involvement.

To add a test project:

```bash
dotnet new xunit -n Reflect.Tests -o Reflect.Tests
dotnet sln Reflect.sln add Reflect.Tests/Reflect.Tests.csproj
dotnet add Reflect.Tests/Reflect.Tests.csproj reference Reflect/Reflect.csproj
```

Note that referencing a MAUI project from a plain test project requires the test project to
target a compatible framework, or the model and service layers to be extracted into a shared
class library. The latter is cleaner and is on the [Roadmap](#roadmap).

Behaviour worth covering first:

- Saving two entries on the same date raises `DuplicateEntryDateException`
- `EntryDate` is normalised to midnight regardless of the time component supplied
- `CreatedAt` survives an update while `UpdatedAt` advances
- Seeding twice does not duplicate reference rows
- Streak calculation across gaps, month boundaries and leap years
- Word count on Markdown containing headings, lists and links
- A correct passphrase verifies and an incorrect one does not

Run tests with:

```bash
dotnet test
```

---

## Building and Publishing

MAUI applications are packaged per platform rather than deployed to a server.

### Windows

Unpackaged build — a plain folder with an `.exe`, runs without Developer Mode:

```bash
dotnet publish Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0 -c Release
```

Self-contained variant, bundling the .NET runtime and Windows App SDK so the target machine
needs nothing pre-installed:

```bash
dotnet publish Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0 -c Release \
  -p:WindowsPackageType=None \
  -p:WindowsAppSDKSelfContained=true \
  -p:SelfContained=true \
  -p:UseMonoRuntime=false \
  -p:RuntimeIdentifier=win-x64
```

`-p:UseMonoRuntime=false` is required. Without it MAUI requests a Mono runtime pack that does
not exist for this version and restore fails.

For an MSIX installer instead, remove `WindowsPackageType=None`. Note that installing an
unsigned MSIX requires Developer Mode or a trusted signing certificate on the target machine.

### Android

```bash
dotnet publish Reflect/Reflect.csproj -f net10.0-android -c Release
```

Produces an APK/AAB under `bin/Release/net10.0-android/publish/`. For a build destined for
distribution, supply your own keystore via `-p:AndroidKeyStore=true` together with the
keystore path, alias and passwords.

### iOS and Mac Catalyst

Both require a macOS build host with Xcode installed, plus an Apple Developer account for
signing. They cannot be built on Windows.

```bash
dotnet publish Reflect/Reflect.csproj -f net10.0-ios -c Release
dotnet publish Reflect/Reflect.csproj -f net10.0-maccatalyst -c Release
```

---

## Troubleshooting

### The app exits immediately with code `-1073741189` (`0xC000027B`)

A stowed WinRT exception, almost always Windows App SDK initialisation failing. The usual
cause is a project configured as packaged (MSIX) but launched as a loose executable, so the
app has no package identity and the Windows App SDK classes are not registered.

This project sets `<WindowsPackageType>None</WindowsPackageType>` specifically to avoid this.
If you hit it, confirm that property is still present.

### `REGDB_E_CLASSNOTREG` / `Class not registered (0x80040154)`

Same root cause as above, surfaced with a clearer stack trace pointing at
`DeploymentManagerAutoInitializer`. Either run unpackaged (the default here), or register the
MSIX layout properly.

### `Deployment failed with HRESULT: 0x80073CFF` — "you need a developer license"

Windows Developer Mode is off, so unsigned MSIX sideloading is blocked. Two options:

- **Preferred:** keep the unpackaged build. It does not need Developer Mode at all.
- Otherwise enable Developer Mode: *Settings → System → For developers → Developer Mode*.

### `NU1102: Unable to find package Microsoft.NETCore.App.Runtime.Mono.win-x64`

A self-contained Windows build is asking for a Mono runtime pack that does not exist for this
version. Add `-p:UseMonoRuntime=false` to the command.

### Build fails on iOS or Mac Catalyst target frameworks

Expected on Windows — those targets need a Mac. Always pass an explicit `-f` for the platform
you are building rather than building all targets at once.

### `NU1903` high-severity vulnerability warnings on restore

The native SQLite library pin has been lost. Confirm `Reflect.csproj` still contains the
direct `SQLitePCLRaw.lib.e_sqlite3` version 2.1.12 reference described in
[Tech Stack](#tech-stack).

### "You cannot write an entry for a future date"

Intended. An entry records a day that has happened, so future dates are refused — otherwise
streaks could be fabricated by writing ahead. Use the Today button or the back arrow.

### Database changes do not appear, or data looks stale

The schema is created once. `sqlite-net-pcl`'s `CreateTablesAsync` adds missing tables and
columns but does not perform destructive migrations. During development the quickest reset is
to close the app and delete `reflect.db3`; it is recreated and reseeded on next launch.

### Build succeeds but stale output runs

MSBuild may reuse existing output when only properties change. Force a clean rebuild:

```bash
dotnet clean Reflect/Reflect.csproj
rm -rf Reflect/bin Reflect/obj
dotnet build Reflect/Reflect.csproj -f net10.0-windows10.0.19041.0
```

---

## Roadmap

Ordered roughly by dependency — earlier items unblock later ones.

Done:

1. ~~**`EntryService` implementation**~~ — CRUD with the one-per-day rule, tag
   synchronisation, word counting, and parameterised search.
2. ~~**Dependency injection wiring**~~ — MudBlazor, `IJournalDatabase` and services registered
   in `MauiProgram`.
3. ~~**Reference data service**~~ — moods, tags and categories exposed to the UI, with custom
   tag creation.
4. ~~**Entry editor**~~ — Markdown with live preview, mood picker enforcing the primary and
   two-secondary rules, tag and category selection.
5. ~~**Calendar view**~~ — month grid marking written days, with that month's entries beside it.
6. ~~**Timeline and search UI**~~ — paginated cards with text search and date, mood, tag and
   category filters, all resolved in SQL.
7. ~~**Streak service**~~ — current streak, longest streak, missed days, with a streak counted
   as alive until a full day passes unwritten.
8. ~~**Analytics dashboard**~~ — mood distribution, frequent moods, tag usage, category
   breakdown and word-count trends, date-range filterable.

9. ~~**Security**~~ — PBKDF2 passphrase set-up and an unlock screen that replaces the whole app.
10. ~~**PDF export**~~ — date-range export via QuestPDF.
11. ~~**Theme persistence**~~ — the choice is stored in `AppSettings.Theme` and survives restarts.

Remaining:

12. **Extract a shared class library** so models and services can be unit tested without the
    MAUI target frameworks, then add the test project. The assertions already exist as
    throwaway harnesses — see [Testing](#testing).

---

## Coursework Mapping

Where each marking-scheme item is or will be satisfied. Kept current as work lands.

| Marking item | Marks | Location | Status |
| --- | --- | --- | --- |
| Journal entry management | 5 | `Services/EntryService.cs`, `Components/Pages/Write.razor` | Done |
| Markdown writing | 5 | `Services/MarkdownRenderer.cs`, editor preview pane | Done |
| Mood tracking | 5 | `Models/Mood.cs`, `Data/SeedData.cs`, editor mood pickers | Done |
| Tagging system | 5 | `Models/Tag.cs`, `Services/ReferenceDataService.cs`, editor | Done |
| Calendar navigation | 5 | `Components/Pages/Calendar.razor` | Done |
| Paginated journal view | 5 | `Components/Pages/Journal.razor`, `Models/PagedResult.cs` | Done |
| Search and filter | 5 | `Components/Pages/Journal.razor`, `Models/EntryQuery.cs` | Done |
| Streak tracking | 5 | `Services/AnalyticsService.cs`, `Components/Pages/Dashboard.razor` | Done |
| Theme customisation | 5 | `MainLayout.razor`, `Components/Pages/Settings.razor` | Done |
| Dashboard analytics | 5 | `Services/AnalyticsService.cs`, `Components/Pages/Dashboard.razor` | Done |
| Security and privacy | 5 | `Services/SettingsService.cs`, `Components/Layout/LockScreen.razor` | Done |
| Export journals | 5 | `Services/PdfJournalExporter.cs`, `Components/Pages/Export.razor` | Done |
| Code readability | 5 | Throughout — XML doc comments, consistent naming | Ongoing |
| Code efficiency | 5 | Indexed columns, stored `WordCount`, cached reference data, paged queries | Ongoing |
| Code modularity | 5 | Interface-per-service, DI, layered structure | Ongoing |
| Error handling | 5 | `DuplicateEntryDateException`, validation at both layers, logging, UI fallbacks | Ongoing |
| Version control | 5 | Conventional Commits, private repository | Ongoing |
| User experience | 5 | MudBlazor, responsive grid, live preview, confirmation on delete | Ongoing |

---

## Academic Integrity

This repository contains coursework submitted for assessment at London Metropolitan
University via Islington College. It is deliberately **private**.

The university treats plagiarism and contract cheating as serious offences, and the penalties
apply to **all parties involved** — including anyone whose work is copied. Please do not
redistribute this code or make it public.

Any external libraries, articles or sources used are credited in the commit history and in
this README.
