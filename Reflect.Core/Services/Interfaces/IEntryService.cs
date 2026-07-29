using Reflect.Models;

namespace Reflect.Services.Interfaces;

// CRUD for entries plus the queries the calendar and timeline need.
public interface IEntryService
{
    Task<JournalEntry?> GetByDateAsync(DateTime date);

    Task<JournalEntry?> GetByIdAsync(int id);

    Task<bool> ExistsForDateAsync(DateTime date);

    // Throws DuplicateEntryDateException if another entry already has that date.
    Task<JournalEntry> SaveAsync(JournalEntry entry, IReadOnlyCollection<int> tagIds);

    Task DeleteAsync(int id);

    // Newest first, one page at a time.
    Task<PagedResult<JournalEntry>> SearchAsync(EntryQuery query, int page, int pageSize);

    // Whole range, oldest first. The calendar and dashboard need all of it at
    // once rather than a page.
    Task<IReadOnlyList<JournalEntry>> GetRangeAsync(DateTime from, DateTime to);

    Task<IReadOnlyList<int>> GetTagIdsAsync(int entryId);

    Task<IReadOnlyList<DateTime>> GetEntryDatesAsync(DateTime from, DateTime to);
}
