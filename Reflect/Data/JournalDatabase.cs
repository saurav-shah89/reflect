using Microsoft.Extensions.Logging;
using Reflect.Models;
using SQLite;

namespace Reflect.Data;

/// <summary>
/// Owns the SQLite connection: creates the file, builds the schema and seeds the
/// fixed reference data exactly once per install.
/// </summary>
public sealed class JournalDatabase : IJournalDatabase
{
    /// <summary>Database file name inside the platform's app-data directory.</summary>
    public const string DatabaseFileName = "reflect.db3";

    private const SQLiteOpenFlags OpenFlags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private readonly ILogger<JournalDatabase> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);

    private SQLiteAsyncConnection? _connection;

    public JournalDatabase(ILogger<JournalDatabase> logger) => _logger = logger;

    /// <summary>Full path to the database file on the current platform.</summary>
    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);

    /// <inheritdoc />
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
            // Re-check inside the lock: a concurrent caller may have finished
            // while this one was waiting.
            if (_connection is not null)
            {
                return _connection;
            }

            var connection = new SQLiteAsyncConnection(DatabasePath, OpenFlags);
            await CreateSchemaAsync(connection).ConfigureAwait(false);
            await SeedAsync(connection).ConfigureAwait(false);

            _connection = connection;
            _logger.LogInformation("Journal database ready at {Path}", DatabasePath);
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

    /// <summary>
    /// Inserts the specification's fixed moods, tags and categories, plus the
    /// settings row. Each block is guarded by a count check so restarting the
    /// app never duplicates reference data.
    /// </summary>
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
