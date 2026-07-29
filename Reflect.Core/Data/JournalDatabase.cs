using Microsoft.Extensions.Logging;
using Reflect.Models;
using SQLite;

namespace Reflect.Data;

// Handles the SQLite connection - creates the file, builds the tables and
// seeds the reference data, once per install.
//
// The path is passed in rather than worked out here. Asking MAUI for the app
// data folder was the only thing in this project that needed MAUI, so passing
// the path in lets the whole library target plain net10.0.
public sealed class JournalDatabase : IJournalDatabase
{
    public const string DatabaseFileName = "reflect.db3";

    private const SQLiteOpenFlags OpenFlags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private readonly string _databasePath;
    private readonly ILogger<JournalDatabase> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private SQLiteAsyncConnection? _connection;

    public JournalDatabase(string databasePath, ILogger<JournalDatabase> logger)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("A database path is required.", nameof(databasePath));
        }

        _databasePath = databasePath;
        _logger = logger;
    }

    public string DatabasePath => _databasePath;

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        // Fast path once initialisation has completed.
        if (_connection is not null)
        {
            return _connection;
        }

        await _initLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // Check again inside the lock - something else may have finished
            // setting up while this one was waiting.
            if (_connection is not null)
            {
                return _connection;
            }

            var connection = new SQLiteAsyncConnection(_databasePath, OpenFlags);
            await CreateSchemaAsync(connection).ConfigureAwait(false);
            await SeedAsync(connection).ConfigureAwait(false);

            _connection = connection;
            _logger.LogInformation("Journal database ready at {Path}", _databasePath);
            return _connection;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static async Task CreateSchemaAsync(SQLiteAsyncConnection connection)
    {
        // Enforce referential integrity at the database level.
        await connection.ExecuteAsync("PRAGMA foreign_keys = ON;").ConfigureAwait(false);

        await connection.CreateTablesAsync<Mood, Tag, Category, JournalEntry>().ConfigureAwait(false);
        await connection.CreateTablesAsync<EntryTag, AppSettings>().ConfigureAwait(false);
    }

    // Inserts the moods, tags and categories from the spec plus the settings
    // row. Each one checks the count first so restarting doesn't duplicate them.
    private async Task SeedAsync(SQLiteAsyncConnection connection)
    {
        if (await connection.Table<Mood>().CountAsync().ConfigureAwait(false) == 0)
        {
            await connection.InsertAllAsync(SeedData.Moods).ConfigureAwait(false);
            _logger.LogInformation("Seeded {Count} moods", SeedData.Moods.Count);
        }

        if (await connection.Table<Tag>().CountAsync().ConfigureAwait(false) == 0)
        {
            var tags = SeedData.TagNames.Select(name => new Tag { Name = name, IsCustom = false });
            await connection.InsertAllAsync(tags).ConfigureAwait(false);
            _logger.LogInformation("Seeded {Count} tags", SeedData.TagNames.Count);
        }

        if (await connection.Table<Category>().CountAsync().ConfigureAwait(false) == 0)
        {
            var categories = SeedData.CategoryNames.Select(name => new Category { Name = name });
            await connection.InsertAllAsync(categories).ConfigureAwait(false);
            _logger.LogInformation("Seeded {Count} categories", SeedData.CategoryNames.Count);
        }

        if (await connection.Table<AppSettings>().CountAsync().ConfigureAwait(false) == 0)
        {
            await connection.InsertAsync(new AppSettings()).ConfigureAwait(false);
        }
    }
}
