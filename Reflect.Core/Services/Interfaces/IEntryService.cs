using Reflect.Models;

namespace Reflect.Services.Interfaces;

/// <summary>
/// Create, read, update and delete journal entries, and query them for the
/// calendar, timeline and search screens.
/// </summary>
public interface IEntryService
{
    /// <summary>Returns the entry for the given day, or null if none exists.</summary>
    Task<JournalEntry?> GetByDateAsync(DateTime date);

    Task<JournalEntry?> GetByIdAsync(int id);

    /// <summary>True when the given day already has an entry.</summary>
    Task<bool> ExistsForDateAsync(DateTime date);

    /// <summary>
    /// Creates or updates the entry for <see cref="JournalEntry.EntryDate"/> and
    /// replaces its tag set.
    /// </summary>
    /// <returns>The saved entry, including its assigned id.</returns>
    /// <exception cref="DuplicateEntryDateException">
    /// A different entry already occupies that day.
    /// </exception>
    Task<JournalEntry> SaveAsync(JournalEntry entry, IReadOnlyCollection<int> tagIds);

    /// <summary>Deletes the entry and its tag links. No-op if it does not exist.</summary>
    Task DeleteAsync(int id);

    /// <summary>Returns matching entries newest first, one page at a time.</summary>
    Task<PagedResult<JournalEntry>> SearchAsync(EntryQuery query, int page, int pageSize);

    /// <summary>
    /// Returns every entry in the date range, oldest first. Used by the calendar
    /// and by analytics, which need whole ranges rather than pages.
    /// </summary>
    Task<IReadOnlyList<JournalEntry>> GetRangeAsync(DateTime from, DateTime to);

    /// <summary>Returns the tag ids attached to an entry.</summary>
    Task<IReadOnlyList<int>> GetTagIdsAsync(int entryId);

    /// <summary>Returns the dates that have entries within the range.</summary>
    Task<IReadOnlyList<DateTime>> GetEntryDatesAsync(DateTime from, DateTime to);
}
