using Microsoft.Extensions.Logging;
using Reflect.Data;
using Reflect.Models;
using Reflect.Services.Interfaces;
using SQLite;

namespace Reflect.Services;

// Entry CRUD and searching.
//
// Two things are handled here so callers don't have to: dates are always cut
// down to midnight, and CreatedAt is kept when an entry is updated. The entry
// and its tags are saved in one transaction, otherwise a failure halfway
// through could leave an entry with the wrong tags on it.
public sealed class EntryService : IEntryService
{
    private readonly IJournalDatabase _database;
    private readonly ILogger<EntryService> _logger;

    public EntryService(IJournalDatabase database, ILogger<EntryService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<JournalEntry?> GetByDateAsync(DateTime date)
    {
        var day = date.Date;
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        return await connection.Table<JournalEntry>()
            .Where(entry => entry.EntryDate == day)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<JournalEntry?> GetByIdAsync(int id)
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        return await connection.Table<JournalEntry>()
            .Where(entry => entry.Id == id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task<bool> ExistsForDateAsync(DateTime date) =>
        await GetByDateAsync(date).ConfigureAwait(false) is not null;

    public async Task<JournalEntry> SaveAsync(JournalEntry entry, IReadOnlyCollection<int> tagIds)
    {
        ArgumentNullException.ThrowIfNull(entry);
        tagIds ??= Array.Empty<int>();

        Validate(entry);
        NormaliseMoodSlots(entry);

        var day = entry.EntryDate.Date;
        var now = DateTime.Now;

        entry.EntryDate = day;
        entry.Title = entry.Title.Trim();
        entry.WordCount = CountWords(entry.Content);
        entry.UpdatedAt = now;

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        // The unique index is what really stops this, but checking here means
        // the editor gets a proper error message instead of a SQLite one.
        var occupant = await connection.Table<JournalEntry>()
            .Where(existing => existing.EntryDate == day)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (occupant is not null && occupant.Id != entry.Id)
        {
            throw new DuplicateEntryDateException(day);
        }

        if (entry.Id == 0)
        {
            entry.CreatedAt = now;
        }
        else
        {
            // Keep the original CreatedAt, in case the caller built a new object
            // instead of loading the existing one.
            var current = await GetByIdAsync(entry.Id).ConfigureAwait(false);
            entry.CreatedAt = current?.CreatedAt ?? now;
        }

        var distinctTagIds = tagIds.Where(id => id > 0).Distinct().ToArray();

        await connection.RunInTransactionAsync(transaction =>
        {
            if (entry.Id == 0)
            {
                transaction.Insert(entry);
            }
            else
            {
                transaction.Update(entry);
            }

            // Just delete them all and re-add. There are only a handful of tags
            // per entry so working out which ones changed isn't worth it.
            transaction.Execute("DELETE FROM entry_tags WHERE EntryId = ?", entry.Id);

            foreach (var tagId in distinctTagIds)
            {
                transaction.Insert(new EntryTag { EntryId = entry.Id, TagId = tagId });
            }
        }).ConfigureAwait(false);

        _logger.LogInformation(
            "Saved entry {EntryId} for {EntryDate:yyyy-MM-dd} with {TagCount} tag(s)",
            entry.Id, entry.EntryDate, distinctTagIds.Length);

        return entry;
    }

    public async Task DeleteAsync(int id)
    {
        if (id <= 0)
        {
            return;
        }

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        await connection.RunInTransactionAsync(transaction =>
        {
            transaction.Execute("DELETE FROM entry_tags WHERE EntryId = ?", id);
            transaction.Execute("DELETE FROM entries WHERE Id = ?", id);
        }).ConfigureAwait(false);

        _logger.LogInformation("Deleted entry {EntryId}", id);
    }

    public async Task<PagedResult<JournalEntry>> SearchAsync(EntryQuery query, int page, int pageSize)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Clamp instead of throwing - if the pager asks for page 0 it should
        // just get the first page.
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var (whereClause, arguments) = BuildWhereClause(query);

        var total = await connection
            .ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM entries {whereClause}", arguments.ToArray())
            .ConfigureAwait(false);

        if (total == 0)
        {
            return PagedResult<JournalEntry>.Empty(pageSize);
        }

        // Asking past the last page returns the last page rather than nothing.
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);
        page = Math.Min(page, totalPages);

        var pageArguments = new List<object>(arguments) { pageSize, (page - 1) * pageSize };

        var items = await connection.QueryAsync<JournalEntry>(
                $"SELECT * FROM entries {whereClause} ORDER BY EntryDate DESC LIMIT ? OFFSET ?",
                pageArguments.ToArray())
            .ConfigureAwait(false);

        return new PagedResult<JournalEntry>(items, total, page, pageSize);
    }

    public async Task<IReadOnlyList<JournalEntry>> GetRangeAsync(DateTime from, DateTime to)
    {
        var (start, end) = OrderRange(from, to);
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        return await connection.QueryAsync<JournalEntry>(
                "SELECT * FROM entries WHERE EntryDate >= ? AND EntryDate <= ? ORDER BY EntryDate",
                start, end)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<int>> GetTagIdsAsync(int entryId)
    {
        if (entryId <= 0)
        {
            return Array.Empty<int>();
        }

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        var rows = await connection.QueryAsync<TagIdRow>(
                "SELECT TagId FROM entry_tags WHERE EntryId = ?", entryId)
            .ConfigureAwait(false);

        return rows.Select(row => row.TagId).ToArray();
    }

    public async Task<IReadOnlyList<DateTime>> GetEntryDatesAsync(DateTime from, DateTime to)
    {
        var (start, end) = OrderRange(from, to);
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        // Only the date column is read - the calendar needs to know which days
        // are filled, not what is in them.
        var rows = await connection.QueryAsync<EntryDateRow>(
                "SELECT EntryDate FROM entries WHERE EntryDate >= ? AND EntryDate <= ? ORDER BY EntryDate",
                start, end)
            .ConfigureAwait(false);

        return rows.Select(row => row.EntryDate).ToArray();
    }

    // Builds the WHERE clause and the arguments that go with it.
    //
    // The only thing ever put into the SQL string itself is "?" placeholders -
    // every value the user typed is passed as a parameter, so this can't be
    // used for SQL injection.
    private static (string Clause, List<object> Arguments) BuildWhereClause(EntryQuery query)
    {
        var conditions = new List<string>();
        var arguments = new List<object>();

        if (!string.IsNullOrWhiteSpace(query.SearchText))
        {
            // Escape the LIKE wildcards, otherwise searching for "100%" matches
            // everything.
            var pattern = $"%{EscapeLikePattern(query.SearchText.Trim())}%";
            conditions.Add(@"(Title LIKE ? ESCAPE '\' OR Content LIKE ? ESCAPE '\')");
            arguments.Add(pattern);
            arguments.Add(pattern);
        }

        if (query.FromDate is not null)
        {
            conditions.Add("EntryDate >= ?");
            arguments.Add(query.FromDate.Value.Date);
        }

        if (query.ToDate is not null)
        {
            conditions.Add("EntryDate <= ?");
            arguments.Add(query.ToDate.Value.Date);
        }

        if (query.MoodIds.Count > 0)
        {
            // An entry matches if the mood appears in any of its three slots.
            var placeholders = BuildPlaceholders(query.MoodIds.Count);
            conditions.Add(
                $"(PrimaryMoodId IN ({placeholders}) " +
                $"OR SecondaryMoodOneId IN ({placeholders}) " +
                $"OR SecondaryMoodTwoId IN ({placeholders}))");

            // The same ids are bound once per slot, in clause order.
            for (var slot = 0; slot < 3; slot++)
            {
                arguments.AddRange(query.MoodIds.Cast<object>());
            }
        }

        if (query.TagIds.Count > 0)
        {
            var placeholders = BuildPlaceholders(query.TagIds.Count);
            conditions.Add(
                "EXISTS (SELECT 1 FROM entry_tags WHERE entry_tags.EntryId = entries.Id " +
                $"AND entry_tags.TagId IN ({placeholders}))");
            arguments.AddRange(query.TagIds.Cast<object>());
        }

        if (query.CategoryId is not null)
        {
            conditions.Add("CategoryId = ?");
            arguments.Add(query.CategoryId.Value);
        }

        var clause = conditions.Count == 0
            ? string.Empty
            : "WHERE " + string.Join(" AND ", conditions);

        return (clause, arguments);
    }

    // Builds "?, ?, ?" for an IN clause.
    private static string BuildPlaceholders(int count) =>
        string.Join(", ", Enumerable.Repeat("?", count));

    // Backslash has to be done first or it escapes the backslashes added below.
    private static string EscapeLikePattern(string value) => value
        .Replace(@"\", @"\\")
        .Replace("%", @"\%")
        .Replace("_", @"\_");

    // Skips tokens with no letters or numbers, so Markdown characters like #,
    // - and > don't get counted as words.
    private static int CountWords(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return 0;
        }

        return content
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Count(token => token.Any(char.IsLetterOrDigit));
    }

    // Swaps the dates round if they came in backwards.
    private static (DateTime Start, DateTime End) OrderRange(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date;

        return start <= end ? (start, end) : (end, start);
    }

    private static void Validate(JournalEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Title))
        {
            throw new ArgumentException("An entry needs a title.", nameof(entry));
        }

        if (entry.Title.Trim().Length > 200)
        {
            throw new ArgumentException("Titles are limited to 200 characters.", nameof(entry));
        }

        if (entry.PrimaryMoodId <= 0)
        {
            throw new ArgumentException("A primary mood is required.", nameof(entry));
        }

        if (entry.EntryDate == default)
        {
            throw new ArgumentException("An entry needs a date.", nameof(entry));
        }
    }

    // Drops secondary moods that repeat the primary or each other, and moves
    // the second one up if the first slot ended up empty.
    private static void NormaliseMoodSlots(JournalEntry entry)
    {
        var secondaries = new[] { entry.SecondaryMoodOneId, entry.SecondaryMoodTwoId }
            .Where(moodId => moodId is > 0 && moodId != entry.PrimaryMoodId)
            .Select(moodId => moodId!.Value)
            .Distinct()
            .ToArray();

        entry.SecondaryMoodOneId = secondaries.Length > 0 ? secondaries[0] : null;
        entry.SecondaryMoodTwoId = secondaries.Length > 1 ? secondaries[1] : null;
    }

    // Used to read back a single column.
    private sealed class TagIdRow
    {
        public int TagId { get; set; }
    }

    private sealed class EntryDateRow
    {
        public DateTime EntryDate { get; set; }
    }
}
