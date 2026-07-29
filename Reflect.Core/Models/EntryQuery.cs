namespace Reflect.Models;

// Filter options for the timeline. Everything is optional, so an empty query
// just returns all the entries.
public sealed class EntryQuery
{
    public string? SearchText { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    // Matches if any of the entry's three mood slots holds one of these.
    public IReadOnlyCollection<int> MoodIds { get; set; } = Array.Empty<int>();

    public IReadOnlyCollection<int> TagIds { get; set; } = Array.Empty<int>();

    public int? CategoryId { get; set; }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(SearchText) &&
        FromDate is null &&
        ToDate is null &&
        MoodIds.Count == 0 &&
        TagIds.Count == 0 &&
        CategoryId is null;
}
