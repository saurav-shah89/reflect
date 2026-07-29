using Microsoft.Extensions.Logging;
using Reflect.Data;
using Reflect.Models;
using Reflect.Services.Interfaces;

namespace Reflect.Services;

// Reads the moods, tags and categories.
//
// These lists are small and almost every screen needs them, so they're cached
// after the first read and the cache is thrown away when a tag or category is
// added. It uses a SemaphoreSlim rather than lock because the database calls
// are async and you can't await inside a lock.
public sealed class ReferenceDataService : IReferenceDataService
{
    private readonly IJournalDatabase _database;
    private readonly ILogger<ReferenceDataService> _logger;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);

    private IReadOnlyList<Mood>? _moods;
    private IReadOnlyDictionary<int, Mood>? _moodLookup;
    private IReadOnlyList<Tag>? _tags;
    private IReadOnlyList<Category>? _categories;

    public ReferenceDataService(IJournalDatabase database, ILogger<ReferenceDataService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Mood>> GetMoodsAsync()
    {
        if (_moods is not null)
        {
            return _moods;
        }

        await _cacheLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_moods is null)
            {
                var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

                _moods = await connection.QueryAsync<Mood>(
                        "SELECT * FROM moods ORDER BY Category, SortOrder")
                    .ConfigureAwait(false);

                _moodLookup = _moods.ToDictionary(mood => mood.Id);
            }

            return _moods;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<IReadOnlyDictionary<int, Mood>> GetMoodLookupAsync()
    {
        // GetMoodsAsync populates both caches together.
        await GetMoodsAsync().ConfigureAwait(false);
        return _moodLookup!;
    }

    public async Task<IReadOnlyList<Tag>> GetTagsAsync()
    {
        if (_tags is not null)
        {
            return _tags;
        }

        await _cacheLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_tags is null)
            {
                var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

                _tags = await connection.QueryAsync<Tag>("SELECT * FROM tags ORDER BY Name")
                    .ConfigureAwait(false);
            }

            return _tags;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync()
    {
        if (_categories is not null)
        {
            return _categories;
        }

        await _cacheLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_categories is null)
            {
                var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

                _categories = await connection
                    .QueryAsync<Category>("SELECT * FROM categories ORDER BY Name")
                    .ConfigureAwait(false);
            }

            return _categories;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<Tag> GetOrCreateTagAsync(string name)
    {
        var trimmed = RequireName(name, nameof(name));
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        // COLLATE NOCASE so "work" finds the existing "Work" rather than adding
        // a second tag that only differs by capitals.
        var existing = await connection
            .QueryAsync<Tag>("SELECT * FROM tags WHERE Name = ? COLLATE NOCASE LIMIT 1", trimmed)
            .ConfigureAwait(false);

        if (existing.Count > 0)
        {
            return existing[0];
        }

        var tag = new Tag { Name = trimmed, IsCustom = true };
        await connection.InsertAsync(tag).ConfigureAwait(false);
        InvalidateTags();

        _logger.LogInformation("Created custom tag {TagName}", trimmed);
        return tag;
    }

    public async Task<Category> GetOrCreateCategoryAsync(string name)
    {
        var trimmed = RequireName(name, nameof(name));
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        var existing = await connection
            .QueryAsync<Category>("SELECT * FROM categories WHERE Name = ? COLLATE NOCASE LIMIT 1", trimmed)
            .ConfigureAwait(false);

        if (existing.Count > 0)
        {
            return existing[0];
        }

        var category = new Category { Name = trimmed };
        await connection.InsertAsync(category).ConfigureAwait(false);
        InvalidateCategories();

        _logger.LogInformation("Created category {CategoryName}", trimmed);
        return category;
    }

    // Trims the name and checks it isn't blank or too long.
    private static string RequireName(string name, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A name is required.", parameterName);
        }

        var trimmed = name.Trim();

        if (trimmed.Length > 40)
        {
            throw new ArgumentException("Names are limited to 40 characters.", parameterName);
        }

        return trimmed;
    }

    private void InvalidateTags() => _tags = null;

    private void InvalidateCategories() => _categories = null;
}
